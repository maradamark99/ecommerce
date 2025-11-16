import { cleanUp, when, PRODUCTCATALOG, createTestCategories, createAndListTestProducts } from '../common.js';
import fs from 'fs/promises';
import path from 'path';

describe('Product catalog search/filter flow', () => {
  let categoryIds;
  let productIds;

  let categories;
  let products;

  afterAll(async () => {
    await cleanUp();
  });
  
  beforeAll(async () => {
    const categoriesPath = path.resolve('data/categories.json');
    const productsPath = path.resolve('data/products.json');
    categories = JSON.parse(await fs.readFile(categoriesPath, 'utf8'));
    products = JSON.parse(await fs.readFile(productsPath, 'utf8'));

    categoryIds = await createTestCategories(categories);
    productIds = await createAndListTestProducts(products, categoryIds);
  });

  it('should have created 3 categories', () => {
    expect(Object.keys(categoryIds).length).toBe(3);
  });

  it('should have created and listed 11 products', () => {
    expect(Object.keys(productIds).length).toBe(11);
  });

  it('should search for products by category', async () => {
    const res = await when()
      .get(PRODUCTCATALOG)
      .query({ categoryId: categoryIds.Books });

    expect(res.statusCode).toBe(200);
    const data = JSON.parse(res.text);
    expect(data.items.length).toBe(3);
    for (const product of data.items) {
      expect(product.categoryId).toBe(categoryIds.Books);
    }
  });

  it('should return not found for non-existing product', async () => {
    const res = await when()
      .get(`${PRODUCTCATALOG}/non-existing-id`);
    
    expect(res.statusCode).toBe(404);
  });

  it('should search for products by search term', async () => {
    const res = await when()
      .get(PRODUCTCATALOG)
      .query({ searchTerm: 'The Great' });

    expect(res.statusCode).toBe(200);
    const data = JSON.parse(res.text);
    expect(data.items.length).toBe(2);
    for (const product of data.items) {
      expect(product.name).toContain('The Great');
    }
  });

  it('should search for products by min price', async () => {
    const res = await when()
      .get(PRODUCTCATALOG)
      .query({ minPrice: 50 });

    expect(res.statusCode).toBe(200);
    const data = JSON.parse(res.text);
    productsWithPriceMoreThanEqualTo50 = products.filter(prod => prod.price >= 50);
    expect(data.items.length).toBeLessThanOrEqual(productsWithPriceMoreThanEqualTo50.length);
    productsWithPriceMoreThanEqualTo50.forEach(prod => {
      expect(data.items.some(item => item.name === prod.name)).toBe(true);
    });
  });

  it('should search for products by min price', async () => {
    const res = await when()
      .get(PRODUCTCATALOG)
      .query({ minPrice: 50 });

    expect(res.statusCode).toBe(200);
    const data = JSON.parse(res.text);
    productsWithPriceMoreThanEqualTo50 = products.filter(prod => prod.price >= 50);
    expect(data.items.length).toBeLessThanOrEqual(productsWithPriceMoreThanEqualTo50.length);
    productsWithPriceMoreThanEqualTo50.forEach(prod => {
      expect(data.items.some(item => item.name === prod.name)).toBe(true);
    });
  });

  it('should search for products with specific attribute', async () => {
    const res = await when()
      .get(PRODUCTCATALOG)
      .query({
        'AttributeFilters[0].Name': 'Color',
        'AttributeFilters[0].Value': 'Black',
        'AttributeFilters[1].Name': 'Size',
        'AttributeFilters[1].Value': 'M'
      });

    expect(res.statusCode).toBe(200);
    const data = JSON.parse(res.text);
    expect(data.items.length).toBe(2);
    data.items.forEach(item => {
      const sizeAttribute = item.attributes.Size;
      expect(sizeAttribute).toBeDefined();
      expect(sizeAttribute).toBe('M');
      const colorAttribute = item.attributes.Color;
      expect(colorAttribute).toBeDefined();
      expect(colorAttribute).toBe('Black');
    });
  });
});