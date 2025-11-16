import { cleanUp, when, CATEGORIES } from '../common.js';

describe('Category Management flow', () => {
  let electronicsCategoryId;
  let laptopsCategoryId;
  let gamingLaptopsCategoryId;

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

    const data = JSON.parse(res.text);
    expect(res.statusCode).toBe(201);
    expect(data.id).toBeDefined();
    electronicsCategoryId = data.id;
  });

  it('should create Gaming Laptops category under Electronics', async () => {
    const res = await when()
      .post(CATEGORIES)
      .send({
        name: 'Gaming Laptops',
        parentId: electronicsCategoryId,
        attributeDefinitions: [
          { name: 'GPU', type: 'string', isRequired: true },
        ]
      });

    expect(res.statusCode).toBe(201);
    const data = JSON.parse(res.text);
    expect(data.id).toBeDefined();
    gamingLaptopsCategoryId = data.id;
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
    const data = JSON.parse(res.text)
    expect(data.id).toBeDefined();
    laptopsCategoryId = data.id;
  });

  it('should update Gaming Laptops category', async () => {
    const res = await when()
      .put(`${CATEGORIES}/${gamingLaptopsCategoryId}`)
      .send({
        name: 'Gaming Laptops',
        parentId: laptopsCategoryId,
        attributeDefinitions: [
          { name: 'GPU', type: 'string', isRequired: true },
          { name: 'Cooling System', type: 'string', isRequired: false }
        ]
      });

    expect(res.statusCode).toBe(200);
  });

  it('should get Gaming Laptops category', async () => {
    const res = await when()
      .get(`${CATEGORIES}/${gamingLaptopsCategoryId}`);

    expect(res.statusCode).toBe(200);
    const data = JSON.parse(res.text);
    expect(data.name).toBe('Gaming Laptops');
    expect(data.attributes).toEqual(
      expect.arrayContaining([
        { name: 'Brand', type: 'STRING', isRequired: true },
        { name: 'Warranty', type: 'NUMERIC', isRequired: false },
        { name: 'RAM', type: 'NUMERIC', isRequired: true },
        { name: 'Storage', type: 'STRING', isRequired: true },
        { name: 'Processor', type: 'STRING', isRequired: true },
        { name: 'GPU', type: 'STRING', isRequired: true },
        { name: 'Cooling System', type: 'STRING', isRequired: false }
      ])
    );
    expect(data.path).toBe(`electronics/laptops/gaming-laptops`);
  });

  it('should list 2 categories per page', async () => {
    const res = await when()
      .get(`${CATEGORIES}?page=1&pageSize=2`);

    expect(res.statusCode).toBe(200);
    const data = JSON.parse(res.text);
    expect(data.items.length).toBe(2);
    expect(data.page).toBe(1);
    expect(data.pageSize).toBe(2);
  });

  it('should delete Laptops category', async () => {
    const res = await when()
      .delete(`${CATEGORIES}/${laptopsCategoryId}`);

    expect(res.statusCode).toBe(204);
  });

  it('should not get Gaming Laptops category', async () => {
    const res = await when()
      .get(`${CATEGORIES}/${gamingLaptopsCategoryId}`);

    expect(res.statusCode).toBe(404);
  });

});