using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.PublicCatalog;

public class PublicNewArrivalProductItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? ShortDescription { get; set; }
    public string Sku { get; set; } = default!;
    public string? Barcode { get; set; }
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public Guid? ModelId { get; set; }
    public string? ModelName { get; set; }
    public Guid? PartTypeId { get; set; }
    public string? PartTypeName { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public int StockQuantity { get; set; }
    public int QuantityInHand { get; set; }
    public bool IsInStock { get; set; }
    public bool IsPriceLocked { get; set; } = true;
    public bool CanOrder { get; set; } = false;
    public string? Slug { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
