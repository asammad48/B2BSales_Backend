namespace B2BSpareParts.Application.DTOs.Lookups;

public class ShopLookupResponseDto : LookupItemResponseDto
{
    public string Code { get; set; } = default!;
    public bool IsMain { get; set; }
    public bool IsActive { get; set; }
}
