using System.Buffers.Text;
using Ecommerce.Common.Data;
using Ecommerce.Domain.Cart.Contract;
using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.Payment;
using Ecommerce.Domain.Shipping;

namespace Ecommerce.Tests.Order
{
    public class OrderDataMapperTests
    {
        
        private OrderDataMapper _orderDataMapper;
        
        [SetUp]
        public void Setup()
        {
            _orderDataMapper = new OrderDataMapper();
        }
        
        [Test]
        public void MapCreateOrderDraftRequestDtoToModel_MapsAllFieldsCorrectly()
        {
            var dto = new CreateRequestDto
            (
                CustomerDetails: new CustomerDetailsDto(
                    FullName: "Alice Smith",
                    Email: "alice@example.com",
                    PhoneNumber: "555-1234"
                ),
                ShippingAddress: new AddressDto("USA", "CA", "LA", "90001", "123 Elm St"),
                BillingAddress: new AddressDto("USA", "CA", "LA", "90001", "123 Elm St"),
                PaymentMethod: "CreditCard",
                ShippingMethod: "Standard",
                CustomerNotes: "Please deliver after 5 PM"
            );

            var mapper = new OrderDataMapper();
            var result = mapper.MapCreateRequestDtoToModel(dto);
            
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.CustomerDetailsRequest.Email, Is.EqualTo(dto.CustomerDetails.Email));
                Assert.That(result.CustomerDetailsRequest.FullName, Is.EqualTo(dto.CustomerDetails.FullName));
                Assert.That(result.CustomerDetailsRequest.PhoneNumber, Is.EqualTo(dto.CustomerDetails.PhoneNumber));

                Assert.That(result.ShippingAddress.Country, Is.EqualTo(dto.ShippingAddress.Country));
                Assert.That(result.ShippingAddress.State, Is.EqualTo(dto.ShippingAddress.State));
                Assert.That(result.ShippingAddress.City, Is.EqualTo(dto.ShippingAddress.City));
                Assert.That(result.ShippingAddress.PostalCode, Is.EqualTo(dto.ShippingAddress.PostalCode));
                Assert.That(result.ShippingAddress.AddressLine, Is.EqualTo(dto.ShippingAddress.AddressLine));
              
                Assert.That(result.BillingAddress.Country, Is.EqualTo(dto.BillingAddress.Country));
                Assert.That(result.BillingAddress.State, Is.EqualTo(dto.BillingAddress.State));
                Assert.That(result.BillingAddress.City, Is.EqualTo(dto.BillingAddress.City));
                Assert.That(result.BillingAddress.PostalCode, Is.EqualTo(dto.BillingAddress.PostalCode));
                Assert.That(result.BillingAddress.AddressLine, Is.EqualTo(dto.BillingAddress.AddressLine));
               
                Assert.That(result.PaymentMethod, Is.EqualTo(PaymentMethod.CreditCard));
                Assert.That(result.ShippingMethod, Is.EqualTo(ShippingMethod.Standard));
                Assert.That(result.CustomerNotes, Is.EqualTo(dto.CustomerNotes));
            });
        }
        
        [Test]
        public void MapCreateOrderDraftRequestDtoToModel_ThrowsArgumentNullException_WhenDtoIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _orderDataMapper.MapCreateRequestDtoToModel(null));
        }

        [Test]
        public void MapOrderSummaryToDto_MapsAllFieldsCorrectly()
        {
            var orderSummary = new OrderSummary
            {
                OrderId = Guid.NewGuid().ToString(),
                Items = new List<OrderItem>(),
                Status = Status.Pending,
                CustomerDetails = new CustomerDetails
                {
                    FullName = "Bob Brown",
                    Email = "bob@example.com",
                    PhoneNumber = "555-9876"
                },
                ShippingAddress = new Address
                {
                    Country = "USA",
                    State = "NY",
                    City = "NYC",
                    PostalCode = "10001",
                    AddressLine = "456 Oak St"
                },
                BillingAddress = new Address
                {
                    Country = "USA",
                    State = "NY",
                    City = "NYC",
                    PostalCode = "10001",
                    AddressLine = "456 Oak St"
                },
                PaymentRate = new PaymentRate() 
                {
                    PaymentMethod = PaymentMethod.CreditCard,
                    Fee = 2.5m
                },
                ShippingRate = new ShippingRate() 
                {
                    Method = ShippingMethod.Standard,
                    EstimatedShippingDate = DateTime.UtcNow.AddDays(3),
                    Fee = 5.0m
                },
            };

            var mapper = new OrderDataMapper();
            var dto = mapper.MapOrderSummaryToDto(orderSummary);

            Assert.Multiple(() =>
            {
                Assert.That(dto.OrderId, Is.EqualTo(orderSummary.OrderId));
                Assert.That(dto.OrderStatus, Is.EqualTo(orderSummary.Status.ToString()));
                Assert.That(dto.CustomerDetails.FullName, Is.EqualTo(orderSummary.CustomerDetails.FullName));
                Assert.That(dto.CustomerDetails.Email, Is.EqualTo(orderSummary.CustomerDetails.Email));
                
                Assert.That(dto.CustomerDetails.PhoneNumber, Is.EqualTo(orderSummary.CustomerDetails.PhoneNumber));
                Assert.That(dto.ShippingAddress.Country, Is.EqualTo(orderSummary.ShippingAddress.Country));
                Assert.That(dto.ShippingAddress.State, Is.EqualTo(orderSummary.ShippingAddress.State));
                Assert.That(dto.ShippingAddress.City, Is.EqualTo(orderSummary.ShippingAddress.City));
                Assert.That(dto.ShippingAddress.PostalCode, Is.EqualTo(orderSummary.ShippingAddress.PostalCode));
                Assert.That(dto.ShippingAddress.AddressLine, Is.EqualTo(orderSummary.ShippingAddress.AddressLine));
                
                Assert.That(dto.BillingAddress?.Country, Is.EqualTo(orderSummary.BillingAddress.Country));
                Assert.That(dto.BillingAddress?.State, Is.EqualTo(orderSummary.BillingAddress.State));
                Assert.That(dto.BillingAddress?.City, Is.EqualTo(orderSummary.BillingAddress.City));
                Assert.That(dto.BillingAddress?.PostalCode, Is.EqualTo(orderSummary.BillingAddress.PostalCode));
                Assert.That(dto.BillingAddress?.AddressLine, Is.EqualTo(orderSummary.BillingAddress.AddressLine));
             
                Assert.That(dto.PaymentRate.PaymentMethod, Is.EqualTo(orderSummary.PaymentRate.PaymentMethod.ToString()));
                Assert.That(dto.PaymentRate.Fee, Is.EqualTo(orderSummary.PaymentRate.Fee));
                Assert.That(dto.ShippingRate.ShippingMethod, Is.EqualTo(orderSummary.ShippingRate.Method.ToString()));
                Assert.That(dto.ShippingRate.Fee, Is.EqualTo(orderSummary.ShippingRate.Fee));
            });
        }
        
        [Test]
        public void MapOrderSummaryToDto_ThrowsArgumentNullException_WhenOrderSummaryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _orderDataMapper.MapOrderSummaryToDto(null));
        }
        
    }
}