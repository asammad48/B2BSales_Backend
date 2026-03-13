using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Themes;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class ThemeService : IThemeService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ThemeService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<ThemeResponseDto> GetAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var theme = await _db.ThemeSettings.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted, ct)
                    ?? throw new AppException("Theme not found", 404);

        return new ThemeResponseDto
        {
            LogoPath = theme.LogoPath,
            PrimaryColor = theme.PrimaryColor,
            SecondaryColor = theme.SecondaryColor,
            AccentColor = theme.AccentColor,
            BannerImagePath = theme.BannerImagePath,
            FontFamily = theme.FontFamily,
            FooterText = theme.FooterText
        };
    }

    public async Task UpdateAsync(UpdateThemeRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var theme = await _db.ThemeSettings.FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted, ct);
        if (theme is null)
        {
            theme = new ThemeSetting { TenantId = tenantId };
            _db.ThemeSettings.Add(theme);
        }

        theme.LogoPath = request.LogoPath;
        theme.PrimaryColor = request.PrimaryColor;
        theme.SecondaryColor = request.SecondaryColor;
        theme.AccentColor = request.AccentColor;
        theme.BannerImagePath = request.BannerImagePath;
        theme.FontFamily = request.FontFamily;
        theme.FooterText = request.FooterText;
        theme.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}
