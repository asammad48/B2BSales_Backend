namespace B2BSpareParts.Application.DTOs.ContactInquiries;

public class CreateContactInquiryResponseDto
{
    public Guid InquiryId { get; set; }
    public string Message { get; set; } = default!;
    public DateTimeOffset SubmittedAt { get; set; }
}
