
using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Products;
using B2BSpareParts.Application.DTOs.PublicCatalog;
using B2BSpareParts.Application.DTOs.Themes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public class PublicStorefrontController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IPublicCatalogService _publicCatalogService;
    private readonly IThemeService _themeService;

    public PublicStorefrontController(
        IProductService productService,
        IPublicCatalogService publicCatalogService,
        IThemeService themeService)
    {
        _productService = productService;
        _publicCatalogService = publicCatalogService;
        _themeService = themeService;
    }

    [HttpGet("storefront/theme")]
    public async Task<ActionResult<ApiResponse<ThemeResponseDto>>> GetTheme(CancellationToken ct)
        => Ok(ApiResponse<ThemeResponseDto>.Ok(await _themeService.GetAsync(ct)));

    [HttpGet("storefront/products")]
    public async Task<ActionResult<ApiResponse<PageResponse<ProductListItemResponseDto>>>> GetProducts([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<ProductListItemResponseDto>>.Ok(await _productService.GetPagedAsync(request, true, ct)));

    [HttpGet("storefront/products/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailResponseDto>>> GetProduct(Guid id, CancellationToken ct)
        => Ok(ApiResponse<ProductDetailResponseDto>.Ok(await _productService.GetByIdAsync(id, true, ct)));

    [HttpGet("catalog/filters")]
    public async Task<ActionResult<ApiResponse<PublicCatalogFiltersResponseDto>>> GetFilters(CancellationToken ct)
        => Ok(ApiResponse<PublicCatalogFiltersResponseDto>.Ok(await _publicCatalogService.GetFiltersAsync(ct)));

    [HttpGet("catalog/products")]
    public async Task<ActionResult<ApiResponse<PageResponse<PublicProductListItemDto>>>> GetPublicProducts([FromQuery] GetPublicProductsRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<PublicProductListItemDto>>.Ok(await _publicCatalogService.GetProductsAsync(request, ct)));
}
