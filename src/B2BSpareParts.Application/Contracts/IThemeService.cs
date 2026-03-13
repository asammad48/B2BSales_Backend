using B2BSpareParts.Application.DTOs.Themes;

namespace B2BSpareParts.Application.Contracts;

public interface IThemeService
{
    Task<ThemeResponseDto> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(UpdateThemeRequestDto request, CancellationToken ct = default);
}
