using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Products;

public class RelatedProductResponseDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = default!;
    public string RelationType { get; set; } = default!;
}
