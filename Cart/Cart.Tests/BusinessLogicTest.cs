using Moq;
using Cart.Contract;
using EcommerceLib.Auth;
using EcommerceLib.Contract.Dto;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;

namespace Cart.Tests
{
    [TestFixture]
    public class BusinessLogicTest
    {
        private Mock<IEventProducer<CartCheckedOutEventDto>> _eventProducerMock;
        private Mock<ICartMapper> _cartMapperMock;
        private Mock<IProductManagementClient> _productManagementClientMock;
        private Mock<ICartStore> _cartStoreMock;
        private CartService _cartService;

        [SetUp]
        public void SetUp()
        {
            _cartMapperMock = new Mock<ICartMapper>();
            _productManagementClientMock = new Mock<IProductManagementClient>();
            _cartStoreMock = new Mock<ICartStore>();
            _eventProducerMock = new Mock<IEventProducer<CartCheckedOutEventDto>>();
            _cartService = new CartService(_eventProducerMock.Object, _cartMapperMock.Object, _productManagementClientMock.Object, _cartStoreMock.Object);
        }

        [Test]
        public void CheckoutAsync_ItemCountIsZero_ShouldThrowBadRequestException()
        {
            // Arrange
            _cartStoreMock.Setup(c => c.GetCartForCustomer(It.IsAny<string>()))
                .Returns([]);
            
            var user = new AppUser() { Id = "customerId" };

            // Act
            // Assert
            Assert.ThrowsAsync<BadRequestException>(async () => await _cartService.CheckoutAsync(user, null));
        }
        
        [Test]
        public async Task CheckoutAsync_HappyCase()
        {
            // Arrange
            const string productId = "prod1";
            const int quantity = 2;
            var checkoutDto = TestDataHelper.CreateCheckoutRequestDto();
            _cartStoreMock.Setup(c => c.GetCartForCustomer(It.IsAny<string>()))
                .Returns([new CartItem(productId, quantity)]);
            var productDto = new ProductDto()
            {
                Id = productId,
                Name = "prod1",
                IsAvailable = true,
                Price = 5,
                PrimaryImageUrl = "example.com",
                Condition = "NEW",
                IsDiscounted = false
            };
            _productManagementClientMock.Setup(p => p.GetProductsByIdsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync([productDto]);
            var cartItemResponse = new CartItemResponse(
                ProductId: productId,
                ProductName: productDto.Name,
                Quantity: quantity,
                Condition: productDto.Condition,
                UnitPrice: productDto.Price,
                ImageUrl: productDto.PrimaryImageUrl,
                IsDiscounted: productDto.IsDiscounted,
                DiscountedPrice: productDto.DiscountedPrice,
                IsAvailable: productDto.IsAvailable
            );
            _cartMapperMock.Setup(c => c.ModelToResponse(It.IsAny<CartItem>(), It.IsAny<ProductDto>()))
                .Returns(cartItemResponse);
            
            var user = new AppUser() { Id = "customerId" };

            // Act
            var result = await _cartService.CheckoutAsync(user, checkoutDto);
            
            // Assert
            _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<CartCheckedOutEventDto>(), It.IsAny<CancellationToken>()), Times.Once);
            _cartStoreMock.Verify(c => c.TryClearCart(It.IsAny<string>()), Times.AtLeastOnce);
            
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckoutId, Is.Not.Null.And.Not.Empty);
            Assert.That(result.CartContent, Is.Not.Null);
            Assert.That(result.CartContent.Items.Count, Is.EqualTo(1));
            Assert.That(result.CartContent.Items[0].ProductId, Is.EqualTo(productId));
            Assert.That(result.CartContent.Items[0].Quantity, Is.EqualTo(quantity));
            Assert.That(result.CustomerDetails.FullName, Is.EqualTo(checkoutDto.CustomerDetails.FullName));
            Assert.That(result.CustomerDetails.Email, Is.EqualTo(checkoutDto.CustomerDetails.Email));
            Assert.That(result.CustomerDetails.PhoneNumber, Is.EqualTo(checkoutDto.CustomerDetails.PhoneNumber));
            Assert.That(result.ShippingAddress.AddressLine, Is.EqualTo(checkoutDto.ShippingAddress.AddressLine));
            Assert.That(result.ShippingAddress.City, Is.EqualTo(checkoutDto.ShippingAddress.City));
            Assert.That(result.ShippingAddress.State, Is.EqualTo(checkoutDto.ShippingAddress.State));
            Assert.That(result.ShippingAddress.PostalCode, Is.EqualTo(checkoutDto.ShippingAddress.PostalCode));
            Assert.That(result.ShippingAddress.Country, Is.EqualTo(checkoutDto.ShippingAddress.Country));

            if (checkoutDto.BillingAddress is not null)
            {
                Assert.That(result.BillingAddress, Is.Not.Null);
                Assert.That(result.BillingAddress.AddressLine, Is.EqualTo(checkoutDto.BillingAddress.AddressLine));
                Assert.That(result.BillingAddress.City, Is.EqualTo(checkoutDto.BillingAddress.City));
                Assert.That(result.BillingAddress.State, Is.EqualTo(checkoutDto.BillingAddress.State));
                Assert.That(result.BillingAddress.PostalCode, Is.EqualTo(checkoutDto.BillingAddress.PostalCode));
                Assert.That(result.BillingAddress.Country, Is.EqualTo(checkoutDto.BillingAddress.Country));
            }
            else
            {
                Assert.That(result.BillingAddress, Is.Null);
            }

            Assert.That(result.ShippingMethod, Is.EqualTo(checkoutDto.ShippingMethod));
            Assert.That(result.PaymentMethod, Is.EqualTo(checkoutDto.PaymentMethod));
            Assert.That(result.CustomerNotes, Is.EqualTo(checkoutDto.CustomerNotes));
            
        }

        [Test]
        public async Task GetByIdAsync_EmptyCart_ReturnsEmptyCartResponse()
        {
            // Arrange
            _cartStoreMock.Setup(c => c.GetCartForCustomer(It.IsAny<string>())).Returns(new List<CartItem>());

            // Act
            var result = await _cartService.GetByIdAsync("customerId");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Items, Is.Empty);
        }

        [Test]
        public async Task GetByIdAsync_NonEmptyCart_ReturnsCartResponseWithItems()
        {
            // Arrange
            var cartItems = new List<CartItem> { new("1",1) };
            _cartStoreMock.Setup(c => c.GetCartForCustomer(It.IsAny<string>())).Returns(cartItems);
            var product = new ProductDto { Id = "1" };
            _productManagementClientMock.Setup(c => c.GetProductsByIdsAsync(It.IsAny<List<string>>())).ReturnsAsync([
                product
            ]);
            _cartMapperMock.Setup(c => c.ModelToResponse(It.IsAny<CartItem>(), It.IsAny<ProductDto>())).Returns(new CartItemResponse(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<decimal?>(),
                It.IsAny<bool>()
                ));

            // Act
            var result = await _cartService.GetByIdAsync("customerId");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Items, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task ModifyCartAsync_NewCart_CreatesCart()
        {
            // Arrange
            _cartStoreMock.Setup(c => c.TryModifyCart(It.IsAny<string>(), It.IsAny<List<CartItem>>()))
                .Returns(true);  
            
            // Act
            await _cartService.ModifyCartAsync("customerId", []);

            // Assert
            _cartStoreMock.Verify(c => c.TryModifyCart(It.IsAny<string>(), It.IsAny<List<CartItem>>()), Times.Once);
        }

        [Test]
        public void ModifyCartAsync_InvalidQuantity_ThrowsBadRequestException()
        {
            // Arrange
            var cartItems = new List<CartItem> { new("",-1) };

            // Act and Assert
            Assert.That(async () => await _cartService.ModifyCartAsync("customerId", cartItems), Throws.Exception.TypeOf<BadRequestException>());
        }

        [Test]
        public void ModifyCartAsync_ModifyCartFailed_ThrowsInternalServerErrorException()
        {
            // Arrange
            _cartStoreMock.Setup(c => c.TryModifyCart(It.IsAny<string>(), It.IsAny<List<CartItem>>())).Returns(false);

            // Act and Assert
            Assert.That(async () => await _cartService.ModifyCartAsync("customerId", new List<CartItem>()), Throws.Exception.TypeOf<InternalServerErrorException>());
        }

        [Test]
        public async Task ClearCartAsync_ClearsCart()
        {
            // Act
            await _cartService.ClearCartAsync("customerId");

            // Assert
            _cartStoreMock.Verify(c => c.TryClearCart(It.IsAny<string>()), Times.Once);
        }

    }
}