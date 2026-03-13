using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Products;

public class CreateProductImageRequestDto
{
    public string FilePath { get; set; } = default!;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
