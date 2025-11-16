namespace EcommerceLib.Contract.Dto;

public class ShippingRateDto
{
    public string Method { get; set; }
    public decimal Fee { get; set; }
    public DateTime? EstimatedShippingDate { get; set; }
}