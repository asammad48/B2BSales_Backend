using B2BSpareParts.Application.DTOs.Auth;

namespace B2BSpareParts.Application.Contracts;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<ClientLoginResponseDto> ClientLoginAsync(ClientLoginRequestDto request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken ct = default);
    Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default);
}
