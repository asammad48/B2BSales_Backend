namespace B2BSpareParts.Application.DTOs.Lookups;

public class LookupBundleResponseDto
{
    public List<LookupItemResponseDto> Categories { get; set; } = [];
    public List<LookupItemResponseDto> Brands { get; set; } = [];
    public List<LookupItemResponseDto> Models { get; set; } = [];
    public List<LookupItemResponseDto> PartTypes { get; set; } = [];
    public List<ShopLookupResponseDto> Shops { get; set; } = [];
    public List<LookupItemResponseDto> Clients { get; set; } = [];
    public List<CurrencyLookupResponseDto> Currencies { get; set; } = [];
    public List<LanguageLookupResponseDto> Languages { get; set; } = [];
}
