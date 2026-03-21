using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Users;
using B2BSpareParts.Common;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
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
        var query = _db.Users.AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(search) || x.Email.ToLower().Contains(search) || (x.Phone != null && x.Phone.ToLower().Contains(search)));
        }

        var projected = query
            .OrderBy(x => x.FullName)
            .Select(x => new UserListItemResponseDto
            {
                Id = x.Id,
                ShopId = x.ShopId,
                ShopName = x.Shop != null ? x.Shop.Name : null,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role,
                IsActive = x.IsActive
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task<UserListItemResponseDto> CreateAsync(CreateUserRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // Check if current user is authorized to create users
        if (_tenantContext.Role != UserRoles.Owner && _tenantContext.Role != UserRoles.Staff)
            throw new AppException("Unauthorized to create users", 403);

        // Check email uniqueness
        var exists = await _db.Users.AnyAsync(x => x.Email.ToLower() == request.Email.ToLower() && !x.IsDeleted, ct);
        if (exists)
            throw new AppException("User with this email already exists", 400);

        // Validate shop belongs to same tenant
        if (request.ShopId.HasValue)
        {
            var shopExists = await _db.Shops.AnyAsync(x => x.Id == request.ShopId && x.TenantId == tenantId && !x.IsDeleted, ct);
            if (!shopExists)
                throw new AppException("Shop not found or belongs to another tenant", 404);
        }

        var user = new AppUser
        {
            TenantId = tenantId,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role,
            ShopId = request.ShopId,
            PreferredLanguageId = request.PreferredLanguageId,
            IsActive = request.IsActive
        };

        var hasher = new PasswordHasher<AppUser>();
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var shopName = request.ShopId.HasValue
            ? await _db.Shops.Where(x => x.Id == request.ShopId).Select(x => x.Name).FirstOrDefaultAsync(ct)
            : null;

        return new UserListItemResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ShopId = user.ShopId,
            ShopName = shopName,
            IsActive = user.IsActive
        };
    }
}
