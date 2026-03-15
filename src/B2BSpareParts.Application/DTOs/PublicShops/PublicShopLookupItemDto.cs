namespace B2BSpareParts.Application.DTOs.PublicShops;

public class PublicShopLookupItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsMain { get; set; }
    public bool IsActive { get; set; }
}
