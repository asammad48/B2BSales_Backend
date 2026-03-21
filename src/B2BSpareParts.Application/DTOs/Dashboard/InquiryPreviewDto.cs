namespace B2BSpareParts.Application.DTOs.Dashboard;

public class InquiryPreviewDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}
