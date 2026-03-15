namespace B2BSpareParts.Application.DTOs.Auth;

public class ClientInfoDto
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PreferredCurrencyCode { get; set; }
    public string? PreferredLanguageCode { get; set; }
    public string Status { get; set; } = default!;
}
