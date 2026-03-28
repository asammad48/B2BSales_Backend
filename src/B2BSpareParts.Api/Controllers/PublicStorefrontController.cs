
using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.ContactInquiries;
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

    private readonly IContactInquiryService _contactInquiryService;

    public PublicStorefrontController(
        IProductService productService,
        IPublicCatalogService publicCatalogService,
        IThemeService themeService,
        IContactInquiryService contactInquiryService)
    {
        _productService = productService;
        _publicCatalogService = publicCatalogService;
        _themeService = themeService;
        _contactInquiryService = contactInquiryService;
    }

    [HttpGet("storefront/theme", Name = "PublicStorefront_GetTheme")]
    public async Task<ActionResult<ApiResponse<ThemeResponseDto>>> GetTheme(CancellationToken ct)
        => Ok(ApiResponse<ThemeResponseDto>.Ok(await _themeService.GetAsync(ct)));

    [HttpGet("storefront/products", Name = "PublicStorefront_GetProducts")]
    public async Task<ActionResult<ApiResponse<PageResponse<ProductListItemResponseDto>>>> GetProducts([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<ProductListItemResponseDto>>.Ok(await _productService.GetPagedAsync(request, ct)));

    [HttpGet("storefront/products/{id:guid}", Name = "PublicStorefront_GetProduct")]
    public async Task<ActionResult<ApiResponse<ProductDetailResponseDto>>> GetProduct(Guid id, CancellationToken ct)
        => Ok(ApiResponse<ProductDetailResponseDto>.Ok(await _productService.GetByIdAsync(id, ct)));

    [HttpGet("catalog/filters", Name = "PublicCatalog_GetFilters")]
    public async Task<ActionResult<ApiResponse<PublicCatalogFiltersResponseDto>>> GetFilters(CancellationToken ct)
        => Ok(ApiResponse<PublicCatalogFiltersResponseDto>.Ok(await _publicCatalogService.GetFiltersAsync(ct)));

    [HttpGet("catalog/lookups", Name = "PublicCatalog_GetLookups")]
    public async Task<ActionResult<ApiResponse<PublicCatalogLookupsResponseDto>>> GetLookups(CancellationToken ct)
        => Ok(ApiResponse<PublicCatalogLookupsResponseDto>.Ok(await _publicCatalogService.GetLookupsAsync(ct)));

    [HttpGet("catalog/products", Name = "PublicCatalog_GetProducts")]
    public async Task<ActionResult<ApiResponse<PageResponse<PublicProductListItemDto>>>> GetPublicProducts([FromQuery] GetPublicProductsRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<PublicProductListItemDto>>.Ok(await _publicCatalogService.GetProductsAsync(request, ct)));

    [HttpGet("products/new-arrivals", Name = "PublicCatalog_GetNewArrivals")]
    public async Task<ActionResult<ApiResponse<PageResponse<PublicNewArrivalProductItemDto>>>> GetNewArrivals([FromQuery] GetPublicProductsRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<PublicNewArrivalProductItemDto>>.Ok(await _publicCatalogService.GetNewArrivalsAsync(request, ct)));

    [HttpGet("products/featured", Name = "PublicCatalog_GetFeaturedProducts")]
    public async Task<ActionResult<ApiResponse<PageResponse<PublicNewArrivalProductItemDto>>>> GetFeaturedProducts([FromQuery] GetFeaturedProductsRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<PublicNewArrivalProductItemDto>>.Ok(await _publicCatalogService.GetFeaturedProductsAsync(request, ct)));

    [HttpPost("contact", Name = "PublicStorefront_CreateContactInquiry")]
    public async Task<ActionResult<ApiResponse<CreateContactInquiryResponseDto>>> CreateContactInquiry([FromBody] CreateContactInquiryRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<CreateContactInquiryResponseDto>.Ok(await _contactInquiryService.CreateAsync(request, ct)));
}
