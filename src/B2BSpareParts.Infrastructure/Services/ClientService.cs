using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Clients;
using B2BSpareParts.Common;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class ClientService : IClientService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ClientService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<ClientResponseDto> CreateAsync(CreateClientRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        if (_tenantContext.Role != UserRoles.Owner && _tenantContext.Role != UserRoles.Staff)
            throw new AppException("Unauthorized to create clients", 403);

        var exists = await _db.Clients.AnyAsync(x => x.Email == request.Email && x.TenantId == tenantId && !x.IsDeleted, ct);
        if (exists)
            throw new AppException("Client with this email already exists", 400);

        var client = new Client
        {
            TenantId = tenantId,
            Name = request.Name,
            BusinessName = request.BusinessName,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            PreferredCurrencyId = request.PreferredCurrencyId,
            PreferredLanguageId = request.PreferredLanguageId,
            Status = request.Status
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);

        return new ClientResponseDto
        {
            Id = client.Id,
            Name = client.Name,
            BusinessName = client.BusinessName,
            Email = client.Email ?? string.Empty,
            Phone = client.Phone ?? string.Empty,
            Status = client.Status.ToString()
        };
    }
}
