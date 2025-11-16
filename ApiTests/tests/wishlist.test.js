import { cleanUp, when, WISHLIST, createTestCategories, createAndListTestProducts } from '../common.js';
import fs from 'fs/promises';
import path from 'path';


describe('Wishlist flow', () => {
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

  it('should add Product to wishlist', async () => {
    const productId = productIds[products[0].name]
    const res = await when()
      .post(`${WISHLIST}/${productId}`)
      .send({});

    expect(res.statusCode).toBe(204);
  });

  it('should get the wishlist', async () => { 
    const res = await when()
      .get(`${WISHLIST}`)
      .send({});

    expect(res.statusCode).toBe(200);
    expect(res.body).toBeDefined();
    expect(res.body.length).toBe(1);
    expect(res.body[0].productId).toBe(productIds[products[0].name]);
  });

  it('should remove the product from the wishlist', async () => {
    const productId = productIds[products[0].name];
    const res = await when()
      .delete(`${WISHLIST}/${productId}`)
      .send({});

    expect(res.statusCode).toBe(204);
  });

  it('should return an empty wishlist after removal', async () => {
    const res = await when()
      .get(WISHLIST)

    expect(res.statusCode).toBe(200);
    expect(res.body).toBeDefined();
    expect(res.body.length).toBe(0);
  });

});