using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace B2BSpareParts.Infrastructure.Documents;

internal sealed class PosInvoiceDocument : IDocument
{
    private readonly PosInvoiceDocumentModel _model;
    private readonly byte[]? _logoBytes;
    private readonly byte[] _barcodeBytes;

    static PosInvoiceDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public PosInvoiceDocument(PosInvoiceDocumentModel model)
    {
        _model = model;
        _logoBytes = TryResolveLogoBytes(model.LogoPath);
        _barcodeBytes = GenerateBarcodeBytes(model.BarcodeValue);
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(24);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span(_model.DisclaimerText).FontSize(9).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(_model.TenantName).Bold().FontSize(18);
                column.Item().Text(_model.ShopName).SemiBold();

                if (!string.IsNullOrWhiteSpace(_model.ShopAddress))
                    column.Item().Text(_model.ShopAddress);

                if (!string.IsNullOrWhiteSpace(_model.ShopPhone))
                    column.Item().Text($"Phone: {_model.ShopPhone}");
            });

            row.ConstantItem(110).Height(60).AlignRight().Element(ComposeLogo);
        });
    }

    private void ComposeLogo(IContainer container)
    {
        if (_logoBytes is { Length: > 0 })
        {
            container.Image(_logoBytes).FitArea();
            return;
        }

        var initials = new string(_model.TenantName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => char.ToUpperInvariant(x[0]))
            .Take(3)
            .ToArray());

        container
            .Background(Colors.Grey.Lighten3)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .AlignCenter()
            .AlignMiddle()
            .Text(string.IsNullOrWhiteSpace(initials) ? "LOGO" : initials)
            .Bold()
            .FontSize(22)
            .FontColor(Colors.Grey.Darken2);
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(14);
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeOrderSummary);
                row.ConstantItem(150).Column(barcodeColumn =>
                {
                    barcodeColumn.Item().Text("Barcode").Bold();
                    barcodeColumn.Item().Image(_barcodeBytes).FitWidth();
                    barcodeColumn.Item().AlignCenter().Text(_model.BarcodeValue).FontSize(9);
                });
            });

            column.Item().Element(ComposeItemsTable);
            column.Item().AlignRight().Width(220).Element(ComposeTotals);
            column.Item().Element(ComposeAttestation);
        });
    }

    private void ComposeOrderSummary(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Invoice / Receipt").Bold().FontSize(16);
            column.Item().Text($"Order Number: {_model.OrderNumber}");
            column.Item().Text($"Created At: {_model.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");

            if (_model.CompletedAt.HasValue)
                column.Item().Text($"Completed At: {_model.CompletedAt.Value:yyyy-MM-dd HH:mm:ss} UTC");

            if (!string.IsNullOrWhiteSpace(_model.ClientName))
                column.Item().Text($"Client: {_model.ClientName}");

            column.Item().Text($"Currency: {_model.CurrencyCode}");
        });
    }

    private void ComposeItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.ConstantColumn(55);
                columns.ConstantColumn(75);
                columns.ConstantColumn(85);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCellStyle).Text("Product").SemiBold();
                header.Cell().Element(HeaderCellStyle).Text("SKU").SemiBold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Qty").SemiBold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Unit Price").SemiBold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Line Total").SemiBold();
            });

            foreach (var item in _model.Items)
            {
                table.Cell().Element(BodyCellStyle).Text(item.ProductName);
                table.Cell().Element(BodyCellStyle).Text(item.Sku);
                table.Cell().Element(BodyCellStyle).AlignRight().Text(item.Quantity.ToString());
                table.Cell().Element(BodyCellStyle).AlignRight().Text(FormatMoney(item.UnitPrice));
                table.Cell().Element(BodyCellStyle).AlignRight().Text(FormatMoney(item.LineTotal));
            }
        });
    }

    private void ComposeTotals(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(column =>
        {
            column.Spacing(4);
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Subtotal");
                row.ConstantItem(90).AlignRight().Text(FormatMoney(_model.Subtotal));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Discount");
                row.ConstantItem(90).AlignRight().Text(FormatMoney(_model.DiscountAmount));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Tax");
                row.ConstantItem(90).AlignRight().Text(FormatMoney(_model.TaxAmount));
            });
            column.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
            {
                row.RelativeItem().Text("Total").Bold();
                row.ConstantItem(90).AlignRight().Text(FormatMoney(_model.TotalAmount)).Bold();
            });
        });
    }

    private void ComposeAttestation(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).MinHeight(90).Padding(12).Column(column =>
            {
                column.Item().Text("Disclaimer").Bold();
                column.Item().PaddingTop(6).Text(_model.DisclaimerText);
            });

            row.ConstantItem(180).PaddingLeft(12).Border(1).BorderColor(Colors.Grey.Lighten2).MinHeight(90).Padding(12).Column(column =>
            {
                column.Item().Text(_model.AttestedStampText).Bold();
                column.Item().PaddingTop(38).BorderTop(1).BorderColor(Colors.Grey.Lighten2).Text("Signature / Stamp").FontSize(9);
            });
        });
    }

    private static IContainer HeaderCellStyle(IContainer container)
        => container.Background(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8);

    private static IContainer BodyCellStyle(IContainer container)
        => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8);

    private string FormatMoney(decimal amount) => $"{amount:0.00} {_model.CurrencyCode}";

    private static byte[] GenerateBarcodeBytes(string value)
    {
        var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(8, darkColorRgba: [0, 0, 0, 255], lightColorRgba: [255, 255, 255, 255]);
    }

    private static byte[]? TryResolveLogoBytes(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
            return null;

        if (logoPath.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = logoPath.IndexOf(',');
            if (commaIndex > -1)
                return Convert.FromBase64String(logoPath[(commaIndex + 1)..]);
        }

        if (Uri.TryCreate(logoPath, UriKind.Absolute, out _))
            return null;

        if (File.Exists(logoPath))
            return File.ReadAllBytes(logoPath);

        var normalizedPath = logoPath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
        var candidatePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), normalizedPath),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "B2BSpareParts.Api", normalizedPath)
        };

        foreach (var candidatePath in candidatePaths)
        {
            if (File.Exists(candidatePath))
                return File.ReadAllBytes(candidatePath);
        }

        return null;
    }
}
