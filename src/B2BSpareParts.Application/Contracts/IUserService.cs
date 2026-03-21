using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.Users;

namespace B2BSpareParts.Application.Contracts;

public interface IUserService
{
    Task<UserProfileResponseDto> GetMeAsync(CancellationToken ct = default);
    Task<PageResponse<UserListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default);
    Task<UserListItemResponseDto> CreateAsync(CreateUserRequestDto request, CancellationToken ct = default);
}
