import { cleanUp, when, CART, ORDERMANAGEMENT, createTestCategories, createAndListTestProducts } from '../common.js';
import fs from 'fs/promises';
import path from 'path';

describe('Order management flow', () => {
  let categoryIds;
  let productIds;

  let categories;
  let products;
  let orderId;
  let order;

  afterAll(async () => {
    await cleanUp();
  }, 30000);

  beforeAll(async () => {
    const categoriesPath = path.resolve('data/categories.json');
    const productsPath = path.resolve('data/products.json');
    const orderPath = path.resolve('data/order.json');
    categories = JSON.parse(await fs.readFile(categoriesPath, 'utf8'));
    products = JSON.parse(await fs.readFile(productsPath, 'utf8'));
    order = JSON.parse(await fs.readFile(orderPath, 'utf8'));

    categoryIds = await createTestCategories(categories);
    productIds = await createAndListTestProducts(products, categoryIds);
  }, 30000);

  it('should have created 3 categories', () => {
    expect(Object.keys(categoryIds).length).toBe(3);
  });

  it('should have created and listed 11 products', () => {
    expect(Object.keys(productIds).length).toBe(11);
  });

  it('should add a product to the Cart', async () => {
    const res = await when()
      .put(CART)
      .send([
      {
        productId: productIds[products[0].name],
        quantity: 2
      },
      {
        productId: productIds[products[1].name],
        quantity: 3
      }
    ]);

    expect(res.statusCode).toBe(204);
  });

  it('should create an order', async () => {
    const res = await when()
      .post(ORDERMANAGEMENT)
      .send({...order});  

    expect(res.statusCode).toBe(201);
    expect(res.body.orderId).toBeDefined();
    orderId = res.body.orderId;
  });

  it('should retrieve the order', async () => {
    const res = await when()
      .get(`${ORDERMANAGEMENT}/${orderId}`);

    expect(res.statusCode).toBe(200);
    expect(res.body.id).toBe(orderId);
    expect(res.body.customerDetails.fullName).toBe(order.customerDetails.fullName);
    expect(res.body.customerDetails.email).toBe(order.customerDetails.email);
    expect(res.body.customerDetails.phoneNumber).toBe(order.customerDetails.phoneNumber);
    expect(res.body.shippingAddress.country).toBe(order.shippingAddress.country);
    expect(res.body.shippingAddress.city).toBe(order.shippingAddress.city);
    expect(res.body.shippingAddress.state).toBe(order.shippingAddress.state);
    expect(res.body.shippingAddress.postalCode).toBe(order.shippingAddress.postalCode); 
    expect(res.body.shippingAddress.addressLine).toBe(order.shippingAddress.addressLine);
    expect(res.body.paymentMethod).toBe(order.paymentMethod);
    expect(res.body.shippingMethod).toBe(order.shippingMethod);
    expect(res.body.customerNotes).toBe(order.customerNotes);
  });

  it('should cancel the order', async () => {
    const res = await when()
      .delete(`${ORDERMANAGEMENT}/${orderId}`);

    expect(res.statusCode).toBe(204);
  });

});
