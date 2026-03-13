namespace B2BSpareParts.Application.DTOs.Users;

public class UserProfileResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ShopId { get; set; }
    public string? ShopName { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string Role { get; set; } = default!;
}
