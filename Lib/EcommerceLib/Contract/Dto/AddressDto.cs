using System.Text.Json.Serialization;

namespace EcommerceLib.Contract.Dto;

public class AddressDto
{
    [JsonPropertyName("addressLine")]
    public string AddressLine { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; }
}