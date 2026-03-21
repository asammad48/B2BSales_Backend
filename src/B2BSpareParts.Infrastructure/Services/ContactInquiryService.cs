using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.ContactInquiries;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class ContactInquiryService : IContactInquiryService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ContactInquiryService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<CreateContactInquiryResponseDto> CreateAsync(CreateContactInquiryRequestDto request, CancellationToken ct = default)
    {
        // For public API, we might need a way to determine the tenant.
        // If it's not provided in the context, we'll need to handle it.
        // Given the instructions "attach tenant if required by architecture", we'll use the current context.
        var tenantId = _tenantContext.TenantId;

        var inquiry = new ContactInquiry
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLower(),
            MobileNo = request.MobileNo.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Status = ContactInquiryStatus.New,
            IsRead = false
        };

        _db.ContactInquiries.Add(inquiry);

        // Optionally create notification for wholeseller
        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = NotificationType.NewInquiry,
            Title = "New Contact Inquiry",
            Message = $"New inquiry from {request.Name}: {request.Subject}",
            RelatedEntityId = inquiry.Id
        });

        await _db.SaveChangesAsync(ct);

        return new CreateContactInquiryResponseDto
        {
            InquiryId = inquiry.Id,
            Message = "Your inquiry has been submitted successfully.",
            SubmittedAt = inquiry.CreatedAt
        };
    }

    public async Task<PageResponse<ContactInquiryListItemDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _db.ContactInquiries.AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(search) || x.Email.ToLower().Contains(search) || x.Subject.ToLower().Contains(search));
        }

        var projected = query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ContactInquiryListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                MobileNo = x.MobileNo,
                Subject = x.Subject,
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt,
                IsRead = x.IsRead
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task<ContactInquiryDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var inquiry = await _db.ContactInquiries.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
                      ?? throw new AppException("Inquiry not found", 404);

        if (!inquiry.IsRead)
        {
            inquiry.IsRead = true;
            inquiry.Status = inquiry.Status == ContactInquiryStatus.New ? ContactInquiryStatus.Read : inquiry.Status;
            await _db.SaveChangesAsync(ct);
        }

        return new ContactInquiryDetailsDto
        {
            Id = inquiry.Id,
            Name = inquiry.Name,
            Email = inquiry.Email,
            MobileNo = inquiry.MobileNo,
            Subject = inquiry.Subject,
            Message = inquiry.Message,
            Status = inquiry.Status.ToString(),
            IsRead = inquiry.IsRead,
            CreatedAt = inquiry.CreatedAt,
            UpdatedAt = inquiry.UpdatedAt
        };
    }

    public async Task UpdateStatusAsync(Guid id, ContactInquiryStatus status, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var inquiry = await _db.ContactInquiries.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
                      ?? throw new AppException("Inquiry not found", 404);

        inquiry.Status = status;
        inquiry.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
