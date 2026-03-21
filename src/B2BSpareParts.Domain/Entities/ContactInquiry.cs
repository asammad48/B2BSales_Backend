using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class ContactInquiry : TenantEntity
{
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string MobileNo { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Message { get; set; } = default!;
    public ContactInquiryStatus Status { get; set; } = ContactInquiryStatus.New;
    public bool IsRead { get; set; }
}

public enum ContactInquiryStatus
{
    New = 1,
    Read = 2,
    Replied = 3,
    Closed = 4
}
