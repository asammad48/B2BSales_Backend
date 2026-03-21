namespace B2BSpareParts.Application.DTOs.ContactInquiries;

public class ContactInquiryListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string MobileNo { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
