
using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Products;
using B2BSpareParts.Application.DTOs.Themes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/storefront")]
public class PublicStorefrontController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IThemeService _themeService;

    public PublicStorefrontController(IProductService productService, IThemeService themeService)
    {
        _productService = productService;
        _themeService = themeService;
    }

    [HttpGet("theme")]
    public async Task<ActionResult<ApiResponse<ThemeResponseDto>>> GetTheme(CancellationToken ct)
        => Ok(ApiResponse<ThemeResponseDto>.Ok(await _themeService.GetAsync(ct)));

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<PageResponse<ProductListItemResponseDto>>>> GetProducts([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<ProductListItemResponseDto>>.Ok(await _productService.GetPagedAsync(request, true, ct)));

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailResponseDto>>> GetProduct(Guid id, CancellationToken ct)
        => Ok(ApiResponse<ProductDetailResponseDto>.Ok(await _productService.GetByIdAsync(id, true, ct)));
}
