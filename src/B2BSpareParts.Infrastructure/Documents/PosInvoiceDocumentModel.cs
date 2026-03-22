namespace B2BSpareParts.Infrastructure.Documents;

internal sealed class PosInvoiceDocumentModel
{
    public string TenantName { get; set; } = default!;
    public string ShopName { get; set; } = default!;
    public string? ShopAddress { get; set; }
    public string? ShopPhone { get; set; }
    public string OrderNumber { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ClientName { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string BarcodeValue { get; set; } = default!;
    public string DisclaimerText { get; set; } = default!;
    public string AttestedStampText { get; set; } = default!;
    public string? LogoPath { get; set; }
    public List<PosInvoiceDocumentItemModel> Items { get; set; } = [];
}

internal sealed class PosInvoiceDocumentItemModel
{
    public string ProductName { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
