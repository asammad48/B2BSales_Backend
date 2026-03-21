using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.TenantCurrency;

namespace B2BSpareParts.Application.Contracts;

public interface ITenantCurrencyService
{
    Task<TenantCurrencySettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task UpdateDefaultSellingCurrencyAsync(UpdateDefaultSellingCurrencyRequestDto request, CancellationToken ct = default);
    Task UpsertExchangeRateAsync(UpsertTenantExchangeRateRequestDto request, CancellationToken ct = default);
    Task<ConvertCurrencyResponseDto> ConvertCurrencyAsync(ConvertCurrencyRequestDto request, CancellationToken ct = default);
}
