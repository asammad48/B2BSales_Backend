namespace B2BSpareParts.Application.DTOs.Auth;

public class ClientLoginResponseDto
{
    public string AccessToken { get; set; } = default!;
    public DateTimeOffset? ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public Guid ClientId { get; set; }
    public ClientInfoDto ClientInfo { get; set; } = default!;
}
