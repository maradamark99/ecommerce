namespace Ecommerce.Domain.Payment;

public static class PaymentEvents
{
    public const string PaymentIntentSucceeded = "payment_intent.succeeded";
    public const string PaymentIntentFailed = "payment_intent.payment_failed";
    public const string PaymentIntentCanceled = "payment_intent.canceled";
    public const string ChargeSucceeded = "charge.succeeded";
    public const string ChargeFailed = "charge.failed";
}