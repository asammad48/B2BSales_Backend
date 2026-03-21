using System.ComponentModel.DataAnnotations;

namespace B2BSpareParts.Application.DTOs.ContactInquiries;

public class CreateContactInquiryRequestDto
{
    [Required]
    public string Name { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string MobileNo { get; set; } = default!;

    [Required]
    public string Subject { get; set; } = default!;

    [Required]
    public string Message { get; set; } = default!;
}
