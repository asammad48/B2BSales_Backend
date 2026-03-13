namespace B2BSpareParts.Application.DTOs.Lookups;

public class CurrencyLookupResponseDto : LookupItemResponseDto
{
    public string Code { get; set; } = default!;
    public string Symbol { get; set; } = default!;
}
