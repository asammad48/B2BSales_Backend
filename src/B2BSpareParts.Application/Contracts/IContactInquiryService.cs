using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.ContactInquiries;
using B2BSpareParts.Domain.Entities;

namespace B2BSpareParts.Application.Contracts;

public interface IContactInquiryService
{
    Task<CreateContactInquiryResponseDto> CreateAsync(CreateContactInquiryRequestDto request, CancellationToken ct = default);
    Task<PageResponse<ContactInquiryListItemDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default);
    Task<ContactInquiryDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, ContactInquiryStatus status, CancellationToken ct = default);
}
