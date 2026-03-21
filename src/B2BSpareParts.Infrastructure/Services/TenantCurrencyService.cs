using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Lookups;
using B2BSpareParts.Application.DTOs.TenantCurrency;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class TenantCurrencyService : ITenantCurrencyService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;

    public TenantCurrencyService(AppDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<TenantCurrencySettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var tenant = await _context.Tenants
            .Include(t => t.DefaultSellingCurrency)
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant == null)
            throw new KeyNotFoundException("Tenant not found.");

        var availableCurrencies = await _context.Currencies
            .Where(c => c.IsActive)
            .Select(c => new CurrencyLookupResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Symbol = c.Symbol
            })
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var exchangeRates = await _context.ExchangeRates
            .Where(er => er.TenantId == tenantId)
            .Select(er => new TenantExchangeRateItemDto
            {
                Id = er.Id,
                FromCurrencyId = er.FromCurrencyId,
                FromCurrencyCode = er.FromCurrency!.Code,
                ToCurrencyId = er.ToCurrencyId,
                ToCurrencyCode = er.ToCurrency!.Code,
                Rate = er.Rate
            })
            .ToListAsync(ct);

        return new TenantCurrencySettingsDto
        {
            DefaultSellingCurrencyId = tenant.DefaultSellingCurrencyId,
            DefaultSellingCurrencyCode = tenant.DefaultSellingCurrency?.Code ?? string.Empty,
            AvailableCurrencies = availableCurrencies,
            ExchangeRates = exchangeRates
        };
    }

    public async Task UpdateDefaultSellingCurrencyAsync(UpdateDefaultSellingCurrencyRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant == null)
            throw new KeyNotFoundException("Tenant not found.");

        var currencyExists = await _context.Currencies.AnyAsync(c => c.Id == request.CurrencyId, ct);
        if (!currencyExists)
            throw new ArgumentException("Selected currency does not exist.");

        tenant.DefaultSellingCurrencyId = request.CurrencyId;
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpsertExchangeRateAsync(UpsertTenantExchangeRateRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        if (request.Rate <= 0)
            throw new ArgumentException("Exchange rate must be greater than zero.");

        if (request.FromCurrencyId == request.ToCurrencyId)
            throw new ArgumentException("From and To currencies must be different.");

        var fromExists = await _context.Currencies.AnyAsync(c => c.Id == request.FromCurrencyId, ct);
        var toExists = await _context.Currencies.AnyAsync(c => c.Id == request.ToCurrencyId, ct);

        if (!fromExists || !toExists)
            throw new ArgumentException("Valid From and To currencies are required.");

        var existingRate = await _context.ExchangeRates
            .FirstOrDefaultAsync(er => er.TenantId == tenantId &&
                                      er.FromCurrencyId == request.FromCurrencyId &&
                                      er.ToCurrencyId == request.ToCurrencyId, ct);

        if (existingRate != null)
        {
            existingRate.Rate = request.Rate;
            existingRate.EffectiveDate = DateTime.UtcNow;
        }
        else
        {
            var newRate = new ExchangeRate
            {
                TenantId = tenantId,
                FromCurrencyId = request.FromCurrencyId,
                ToCurrencyId = request.ToCurrencyId,
                Rate = request.Rate,
                EffectiveDate = DateTime.UtcNow
            };
            _context.ExchangeRates.Add(newRate);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<ConvertCurrencyResponseDto> ConvertCurrencyAsync(ConvertCurrencyRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        if (request.FromCurrencyId == request.ToCurrencyId)
        {
            var currency = await _context.Currencies.FindAsync(new object[] { request.FromCurrencyId }, ct);
            return new ConvertCurrencyResponseDto
            {
                ConvertedAmount = request.Amount,
                Rate = 1.0m,
                FromCurrencyCode = currency?.Code ?? string.Empty,
                ToCurrencyCode = currency?.Code ?? string.Empty
            };
        }

        var exchangeRate = await _context.ExchangeRates
            .Include(er => er.FromCurrency)
            .Include(er => er.ToCurrency)
            .FirstOrDefaultAsync(er => er.TenantId == tenantId &&
                                      er.FromCurrencyId == request.FromCurrencyId &&
                                      er.ToCurrencyId == request.ToCurrencyId, ct);

        if (exchangeRate == null)
            throw new InvalidOperationException("Exchange rate not found for the selected currency pair.");

        return new ConvertCurrencyResponseDto
        {
            ConvertedAmount = request.Amount * exchangeRate.Rate,
            Rate = exchangeRate.Rate,
            FromCurrencyCode = exchangeRate.FromCurrency?.Code ?? string.Empty,
            ToCurrencyCode = exchangeRate.ToCurrency?.Code ?? string.Empty
        };
    }
}
