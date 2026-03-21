using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.ContactInquiries;
using B2BSpareParts.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/contact-inquiries")]
[Authorize]
public class ContactInquiriesController : ControllerBase
{
    private readonly IContactInquiryService _contactInquiryService;

    public ContactInquiriesController(IContactInquiryService contactInquiryService)
    {
        _contactInquiryService = contactInquiryService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<ContactInquiryListItemDto>>>> GetPaged([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<ContactInquiryListItemDto>>.Ok(await _contactInquiryService.GetPagedAsync(request, ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ContactInquiryDetailsDto>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<ContactInquiryDetailsDto>.Ok(await _contactInquiryService.GetByIdAsync(id, ct)));

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateStatus(Guid id, [FromBody] B2BSpareParts.Application.DTOs.ContactInquiries.UpdateContactInquiryStatusRequestDto request, CancellationToken ct)
    {
        await _contactInquiryService.UpdateStatusAsync(id, request.Status, ct);
        return Ok(ApiResponse<string>.Ok("Status updated successfully"));
    }
}
