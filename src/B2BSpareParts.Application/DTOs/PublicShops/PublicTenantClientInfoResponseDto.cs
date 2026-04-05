namespace B2BSpareParts.Application.DTOs.PublicShops;

public class PublicTenantClientInfoResponseDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = default!;
    public string TenantCode { get; set; } = default!;
    public string DefaultCurrencyCode { get; set; } = default!;
    public string DefaultCurrencySymbol { get; set; } = default!;
    public List<PublicClientInfoItemDto> Clients { get; set; } = [];
}

public class PublicClientInfoItemDto
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = default!;
    public string? CurrencyCode { get; set; }
    public string? CurrencySymbol { get; set; }
}
