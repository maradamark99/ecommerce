import request from 'supertest';

export const API_BASE_URL = "http://localhost:5277";
export const CATEGORIES = '/api/v1/categories';
export const PRODUCTMANAGEMENT = '/api/v1/product-management';
export const PRODUCTCATALOG = '/api/v1/products';
export const CART = '/api/v1/cart';
export const WISHLIST = '/api/v1/wishlist';
export const ORDERMANAGEMENT = '/api/v1/order-management';
export const PROFILE = '/api/v1/profile';
export const PAYMENTS = '/api/v1/payments';
export const REVIEWS = '/api/v1/reviews';

export function when(url = API_BASE_URL) {
    return request(url)
}

export function cleanUp() {
    return when()
        .post('/api/test/cleanup')
        .send({});
}

export async function createTestCategories(categories) {
    const categoryIds = {};
    for (const cat of categories) {
        const res = await when()
            .post(CATEGORIES)
            .send({
                name: cat.name,
                parentId: null,
                attributeDefinitions: cat.attributeDefinitions
            });
        categoryIds[cat.name] = res.body.id;
    }
    return categoryIds;
}

export async function createAndListTestProducts(products, categoryIds) {
    const productIds = {};
    for (const prod of products) {
      const res = await when().post(PRODUCTMANAGEMENT).send({
        name: prod.name,
        description: prod.description,
        categoryId: categoryIds[prod.category],
        attributes: prod.attributes,
        productCondition: 'New'
      });
      const productId = res.body.id;
      productIds[prod.name] = productId;

      await when().post(`${PRODUCTMANAGEMENT}/listing`).send({
        productId,
        priceInEur: prod.price
      });
    }
    return productIds;
}