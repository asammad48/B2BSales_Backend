namespace B2BSpareParts.Application.DTOs.Auth;

public class ClientLoginRequestDto
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}
