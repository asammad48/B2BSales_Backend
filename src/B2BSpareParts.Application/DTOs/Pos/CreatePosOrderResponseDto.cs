namespace B2BSpareParts.Application.DTOs.Pos;

public class CreatePosOrderResponseDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string ShopName { get; set; } = default!;
    public string? ClientName { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string InvoicePdfUrl { get; set; } = default!;
    public string BarcodeValue { get; set; } = default!;
    public string? LogoUrl { get; set; }
    public string DisclaimerText { get; set; } = default!;
    public string AttestedStampText { get; set; } = default!;
    public List<CreatePosOrderResponseItemDto> Items { get; set; } = [];
}
