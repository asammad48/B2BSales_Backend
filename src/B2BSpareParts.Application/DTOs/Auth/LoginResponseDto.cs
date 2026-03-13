namespace B2BSpareParts.Application.DTOs.Auth;

public class LoginResponseDto
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string Token { get; set; } = default!;
}
