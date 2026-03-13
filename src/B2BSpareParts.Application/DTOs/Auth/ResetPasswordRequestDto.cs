namespace B2BSpareParts.Application.DTOs.Auth;

public class ResetPasswordRequestDto
{
    public string Email { get; set; } = default!;
    public string Token { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
