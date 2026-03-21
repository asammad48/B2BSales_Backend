using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.TenantCurrency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[Authorize(Roles = "Owner,Staff")]
[ApiController]
[Route("api/tenant-currency")]
public class TenantCurrencyController : ControllerBase
{
    private readonly ITenantCurrencyService _tenantCurrencyService;

    public TenantCurrencyController(ITenantCurrencyService tenantCurrencyService)
    {
        _tenantCurrencyService = tenantCurrencyService;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<TenantCurrencySettingsDto>>> GetSettings(CancellationToken ct)
    {
        var settings = await _tenantCurrencyService.GetSettingsAsync(ct);
        return Ok(ApiResponse<TenantCurrencySettingsDto>.Ok(settings));
    }

    [HttpPut("default-selling-currency")]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateDefaultSellingCurrency(UpdateDefaultSellingCurrencyRequestDto request, CancellationToken ct)
    {
        await _tenantCurrencyService.UpdateDefaultSellingCurrencyAsync(request, ct);
        return Ok(ApiResponse<object?>.Ok(null, "Default selling currency updated successfully."));
    }

    [HttpPost("exchange-rates")]
    public async Task<ActionResult<ApiResponse<object?>>> UpsertExchangeRate(UpsertTenantExchangeRateRequestDto request, CancellationToken ct)
    {
        await _tenantCurrencyService.UpsertExchangeRateAsync(request, ct);
        return Ok(ApiResponse<object?>.Ok(null, "Exchange rate updated successfully."));
    }

    [HttpPost("convert")]
    public async Task<ActionResult<ApiResponse<ConvertCurrencyResponseDto>>> ConvertCurrency(ConvertCurrencyRequestDto request, CancellationToken ct)
    {
        var result = await _tenantCurrencyService.ConvertCurrencyAsync(request, ct);
        return Ok(ApiResponse<ConvertCurrencyResponseDto>.Ok(result));
    }
}
