namespace B2BSpareParts.Application.DTOs.Auth;

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
