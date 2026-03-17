namespace B2BSpareParts.Application.DTOs.Shops;

public class ShopLookupItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public bool IsMain { get; set; }
    public bool IsActive { get; set; }
}
