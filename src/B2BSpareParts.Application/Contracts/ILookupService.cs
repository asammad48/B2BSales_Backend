using B2BSpareParts.Application.DTOs.Lookups;

namespace B2BSpareParts.Application.Contracts;

public interface ILookupService
{
    Task<LookupBundleResponseDto> GetBundleAsync(CancellationToken ct = default);
}
