using B2BSpareParts.Domain.Entities;

namespace B2BSpareParts.Application.DTOs.ContactInquiries;

public class UpdateContactInquiryStatusRequestDto
{
    public ContactInquiryStatus Status { get; set; }
}
