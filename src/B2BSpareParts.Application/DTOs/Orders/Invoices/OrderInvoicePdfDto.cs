namespace B2BSpareParts.Application.DTOs.Orders.Invoices;

public class OrderInvoicePdfDto
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = "application/pdf";
    public byte[] Content { get; set; } = [];
}
