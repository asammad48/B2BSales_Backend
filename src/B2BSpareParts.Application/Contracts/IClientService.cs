using B2BSpareParts.Application.DTOs.Clients;

namespace B2BSpareParts.Application.Contracts;

public interface IClientService
{
    Task<ClientResponseDto> CreateAsync(CreateClientRequestDto request, CancellationToken ct = default);
}
