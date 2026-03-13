namespace B2BSpareParts.Application.DTOs.Themes;

public class ThemeResponseDto
{
    public string? LogoPath { get; set; }
    public string PrimaryColor { get; set; } = default!;
    public string SecondaryColor { get; set; } = default!;
    public string AccentColor { get; set; } = default!;
    public string? BannerImagePath { get; set; }
    public string? FontFamily { get; set; }
    public string? FooterText { get; set; }
}
