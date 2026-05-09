namespace Northwind.Application.DTOs;

public class AddressValidationRequestDto
{
    public string Address { get; set; } = string.Empty;
}

public class AddressValidationResultDto
{
    public string FormattedAddress { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsValid { get; set; }
    public string[] Issues { get; set; } = [];
}
