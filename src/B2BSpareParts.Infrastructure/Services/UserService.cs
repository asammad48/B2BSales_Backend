using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Users;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public UserService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<UserProfileResponseDto> GetMeAsync(CancellationToken ct = default)
    {
        var userId = _tenantContext.UserId ?? throw new AppException("Unauthorized", 401);
        var user = await _db.Users.Include(x => x.Shop).FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct)
                   ?? throw new AppException("User not found", 404);

        return new UserProfileResponseDto
        {
            Id = user.Id,
            TenantId = user.TenantId,
            ShopId = user.ShopId,
            ShopName = user.Shop?.Name,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role
        };
    }

    public async Task<PageResponse<UserListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _db.Users.AsNoTracking().Include(x => x.Shop).Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(search) || x.Email.ToLower().Contains(search) || (x.Phone != null && x.Phone.ToLower().Contains(search)));
        }

        var projected = query
            .Select(x => new UserListItemResponseDto
            {
                Id = x.Id,
                ShopId = x.ShopId,
                ShopName = x.Shop != null ? x.Shop.Name : null,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role,
                IsActive = x.IsActive
            })
            .OrderBy(x => x.FullName);

        return await projected.ToPageAsync(request, ct);
    }
}
