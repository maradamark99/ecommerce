import { cleanUp, when, CATEGORIES, PRODUCTMANAGEMENT } from '../common.js';

describe('List product flow', () => {
  let electronicsCategoryId;
  let laptopsCategoryId;
  let productId;
  let mediaId;

  afterAll(async () => {
    await cleanUp();
  });

  it('should create Electronics category', async () => {
    const res = await when()
      .post(CATEGORIES)
      .send({
        name: 'Electronics',
        parentId: null,
        attributeDefinitions: [
          { name: 'Brand', type: 'string', isRequired: true },
          { name: 'Warranty', type: 'numeric', isRequired: false }
        ]
      });

    expect(res.statusCode).toBe(201);
    expect(res.body.id).toBeDefined();
    electronicsCategoryId = res.body.id;
  });

  it('should create Laptops category under Electronics', async () => {
    const res = await when()
      .post(CATEGORIES)
      .send({
        name: 'Laptops',
        parentId: electronicsCategoryId,
        attributeDefinitions: [
          { name: 'RAM', type: 'numeric', isRequired: true },
          { name: 'Storage', type: 'string', isRequired: true },
          { name: 'Processor', type: 'string', isRequired: true }
        ]
      });

    expect(res.statusCode).toBe(201);
    expect(res.body.id).toBeDefined();
    laptopsCategoryId = res.body.id;
  });

  it('should create Gaming Laptop product', async () => {
    const res = await when()
      .post(PRODUCTMANAGEMENT)
      .send({
        name: 'Gaming Laptop',
        description: 'High performance gaming laptop',
        categoryId: laptopsCategoryId,
        attributes: [
          { name: 'Brand', value: 'Alienware' },
          { name: 'Warranty', value: '2' },
          { name: 'RAM', value: '16' },
          { name: 'Storage', value: '512GB SSD' },
          { name: 'Processor', value: 'Intel Core i7' }
        ],
        productCondition: 'New'
      });
    expect(res.statusCode).toBe(201);
    expect(res.body.id).toBeDefined();
    productId = res.body.id;
  });

  it('should add product media', async () => {
    const res = await when()
      .post(`${PRODUCTMANAGEMENT}/media/${productId}?isPrimaryImage=true`)
      .attach('file', 'assets/img.png')

    expect(res.statusCode).toBe(201);
    expect(res.body.id).toBeDefined();
    mediaId = res.body.id;
    expect(res.body.url).toBeDefined();
    expect(res.body.isPrimaryImage).toBe(true);

  });

  it('should delete product media', async () => {
    const res = await when()
      .delete(`${PRODUCTMANAGEMENT}/media/${productId}/${mediaId}`);
    expect(res.statusCode).toBe(204);
  });

  it('should list Gaming Laptop product', async () => {
    const res = await when()
      .post(`${PRODUCTMANAGEMENT}/listing`)
      .send({
        productId: productId,
        priceInEur: 1299.99
      });

    expect(res.statusCode).toBe(200);
    expect(res.body).toBeDefined();
  });

  it('should return listing history for Gaming Laptop', async () => {
        const res = await when()
        .get(`${PRODUCTMANAGEMENT}/listing/${productId}`);

        expect(res.statusCode).toBe(200);
        expect(res.body).toBeDefined();
        expect(res.body).toHaveLength(1);
        expect(res.body[0].productId).toBe(productId);
        expect(res.body[0].priceInEur).toBe(1299.99);
        expect(res.body[0].isActive).toBe(true);
        expect(res.body[0].createdAt).toBeDefined();
        expect(res.body[0].updatedAt).toBeDefined();
  });

  it('should delist Gaming Laptop listing', async () => {
    const res = await when()
      .delete(`${PRODUCTMANAGEMENT}/listing/${productId}`);

    expect(res.statusCode).toBe(204);
  });

});