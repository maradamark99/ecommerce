import { cleanUp, when, PROFILE } from '../common.js';

describe('Profile flow', () => {

  const customerDetails = {
        fullName: "Jane Doe",
        email: "jane.doe@example.com",
        phoneNumber: "+1-555-123-4567",
        defaultShippingAddress: {
          country: "United States",
          state: "California",
          city: "Los Angeles",
          postalCode: "90001",
          addressLine: "123 Main St Apt 4B"
        },
        defaultBillingAddress: {
          country: "United States",
          state: "California",
          city: "Los Angeles",
          postalCode: "90002",
          addressLine: "456 Market St Suite 200"
        }
      }

  afterAll(async () => {
    await cleanUp();
  });

  it('should fill out the users profile', async () => {
    const res = await when()
      .put(`${PROFILE}/me`)
      .send(customerDetails);

    expect(res.statusCode).toBe(204);
  });

  it('should retrieve the user profile', async () => {
    const res = await when()
      .get(`${PROFILE}/me`);

    expect(res.statusCode).toBe(200);
    expect(res.body).toBeDefined();
    expect(res.body.fullName).toBe(customerDetails.fullName);
    expect(res.body.email).toBe(customerDetails.email);
    expect(res.body.phoneNumber).toBe(customerDetails.phoneNumber);
    expect(res.body.defaultShippingAddress.country).toBe(customerDetails.defaultShippingAddress.country);
    expect(res.body.defaultShippingAddress.state).toBe(customerDetails.defaultShippingAddress.state);
    expect(res.body.defaultShippingAddress.city).toBe(customerDetails.defaultShippingAddress.city);
    expect(res.body.defaultShippingAddress.postalCode).toBe(customerDetails.defaultShippingAddress.postalCode);
    expect(res.body.defaultShippingAddress.addressLine).toBe(customerDetails.defaultShippingAddress.addressLine);
    expect(res.body.defaultBillingAddress.country).toBe(customerDetails.defaultBillingAddress.country);
    expect(res.body.defaultBillingAddress.state).toBe(customerDetails.defaultBillingAddress.state);
    expect(res.body.defaultBillingAddress.city).toBe(customerDetails.defaultBillingAddress.city);
    expect(res.body.defaultBillingAddress.postalCode).toBe(customerDetails.defaultBillingAddress.postalCode);
    expect(res.body.defaultBillingAddress.addressLine).toBe(customerDetails.defaultBillingAddress.addressLine);
  });
});