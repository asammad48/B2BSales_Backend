using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Auth;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Infrastructure.Auth;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly JwtTokenGenerator _tokenGenerator;

    public AuthService(AppDbContext db, ITenantContext tenantContext, JwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _tenantContext = tenantContext;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email && !x.IsDeleted, ct)
                   ?? throw new AppException("Invalid credentials", 401);

        var hasher = new PasswordHasher<AppUser>();
        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
            throw new AppException("Invalid credentials", 401);

        return new LoginResponseDto
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Token = _tokenGenerator.Generate(user)
        };
    }

    public async Task ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken ct = default)
    {
        var userId = _tenantContext.UserId ?? throw new AppException("Unauthorized", 401);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct)
                   ?? throw new AppException("User not found", 404);

        var hasher = new PasswordHasher<AppUser>();
        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
            throw new AppException("Current password is invalid", 400);

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email && !x.IsDeleted, ct);
        if (user is null)
        {
            return new ForgotPasswordResponseDto { Message = "If the email exists, a reset token has been created." };
        }

        var token = Guid.NewGuid().ToString("N");
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            TenantId = user.TenantId,
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(2)
        });
        await _db.SaveChangesAsync(ct);

        return new ForgotPasswordResponseDto
        {
            Message = "Reset token created. In production, send this by email.",
            ResetToken = token
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email && !x.IsDeleted, ct)
                   ?? throw new AppException("Invalid request", 400);

        var token = await _db.PasswordResetTokens.FirstOrDefaultAsync(x =>
            x.UserId == user.Id &&
            x.Token == request.Token &&
            !x.IsUsed &&
            x.ExpiresAt > DateTimeOffset.UtcNow &&
            !x.IsDeleted, ct);

        if (token is null)
            throw new AppException("Invalid or expired token", 400);

        var hasher = new PasswordHasher<AppUser>();
        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        token.IsUsed = true;
        await _db.SaveChangesAsync(ct);
    }
}
