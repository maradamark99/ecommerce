import { cleanUp, when, CART, createTestCategories, createAndListTestProducts } from '../common.js';
import fs from 'fs/promises';
import path from 'path';

describe('Cart flow', () => {
  let categoryIds = {};
  let productIds = {};

  let categories;
  let products;

  beforeAll(async () => {
    const categoriesPath = path.resolve('data/categories.json');
    const productsPath = path.resolve('data/products.json');
    categories = JSON.parse(await fs.readFile(categoriesPath, 'utf8'));
    products = JSON.parse(await fs.readFile(productsPath, 'utf8'));

    categoryIds = await createTestCategories(categories); 
    productIds = await createAndListTestProducts(products, categoryIds);
  });

  afterAll(async () => {
    await cleanUp();
  });

  it('should create a Cart if it does not exist yet', async () => {
    const res = await when()
      .put(CART)
      .send([]);
    
    expect(res.statusCode).toBe(204);
    expect(res.text).toBeDefined();
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

  it('should retrieve the Cart', async () => {
    const res = await when()
      .get(CART);

    const data = res.body;
    expect(res.statusCode).toBe(200);
    expect(data).toBeDefined();
    expect(data.products).toHaveLength(2);
    expect(data.products[0].productId).toBe(productIds[products[0].name]);
    expect(data.products[0].quantity).toBe(2);
    expect(data.products[1].productId).toBe(productIds[products[1].name]);
    expect(data.products[1].quantity).toBe(3);
  });

  it('should modify a product in the Cart', async () => {
    const res = await when()
      .put(CART)
      .send([
      {
        productId: productIds[products[0].name],
        quantity: 0
      },
      {
        productId: productIds[products[1].name],
        quantity: 2
      }
    ]);

    expect(res.statusCode).toBe(204);
  });

  it('should retrieve the Cart with modified products', async () => {
    const res = await when()
      .get(CART);

    const data = res.body;
    expect(res.statusCode).toBe(200);
    expect(data).toBeDefined();
    expect(data.products).toHaveLength(1);
    expect(data.products[0].productId).toBe(productIds[products[1].name]);
    expect(data.products[0].quantity).toBe(2);
  });

  it('should clear the Cart', async () => { 
    const res = await when()
      .delete(CART);

    expect(res.statusCode).toBe(204);
  });

  it('should not contain any products', async () => {
    const res = await when()
      .get(CART);

    const data = res.body;
    expect(res.statusCode).toBe(200);
    expect(data).toBeDefined();
    expect(data.products).toHaveLength(0);
  });

});