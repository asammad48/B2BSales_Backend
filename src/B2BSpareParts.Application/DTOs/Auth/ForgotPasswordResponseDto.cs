namespace B2BSpareParts.Application.DTOs.Auth;

public class ForgotPasswordResponseDto
{
    public string Message { get; set; } = default!;
    public string? ResetToken { get; set; }
}
