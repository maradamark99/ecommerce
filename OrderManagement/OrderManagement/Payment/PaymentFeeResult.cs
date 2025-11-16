using System.Net;

namespace OrderManagement.Payment;

public class PaymentFeeResult
{
    public HttpStatusCode StatusCode { get; set; }  
    public PaymentFeeDto? Fee { get; set; }
}