using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class ThemeSetting : TenantEntity
{
    public string? LogoPath { get; set; }
    public string PrimaryColor { get; set; } = "#111827";
    public string SecondaryColor { get; set; } = "#F59E0B";
    public string AccentColor { get; set; } = "#2563EB";
    public string? BannerImagePath { get; set; }
    public string? FontFamily { get; set; }
    public string? FooterText { get; set; }
}
