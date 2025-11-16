import { cleanUp, when, CART, ORDERMANAGEMENT, PAYMENTS, createTestCategories, createAndListTestProducts } from '../common.js';
import fs from 'fs/promises';
import path from 'path';

describe('Payment flow', () => {
  let categoryIds;
  let productIds;

  let categories;
  let products;
  let cartId;
  let orderId;
  let order;

  afterAll(async () => {
    await cleanUp();
  });

  beforeAll(async () => {
    const categoriesPath = path.resolve('data/categories.json');
    const productsPath = path.resolve('data/products.json');
    const orderPath = path.resolve('data/order.json');
    categories = JSON.parse(await fs.readFile(categoriesPath, 'utf8'));
    products = JSON.parse(await fs.readFile(productsPath, 'utf8'));
    order = JSON.parse(await fs.readFile(orderPath, 'utf8'));

    categoryIds = await createTestCategories(categories);
    productIds = await createAndListTestProducts(products, categoryIds);
  });

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
    const orderRequest = {
      cartId: cartId,
      ...order
    };
    const res = await when()
      .post(ORDERMANAGEMENT)
      .send(orderRequest);

    expect(res.statusCode).toBe(201);
    expect(res.body.orderId).toBeDefined();
    orderId = res.body.orderId;
  });

  it('should initiate Cash on Delivery payment', async () => {
    const res = await when()
      .post(`${PAYMENTS}/initiate`)
      .send({ 
        orderId: orderId,
        paymentMethod: 'CashOnDelivery' 
      });

    expect(res.statusCode).toBe(200);
  });

});