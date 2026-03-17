using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<ApiResponse<PageResponse<ProductListItemResponseDto>>>> GetPublic([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<ProductListItemResponseDto>>.Ok(await _productService.GetPagedAsync(request, ct)));

    [AllowAnonymous]
    [HttpGet("public/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailResponseDto>>> GetPublicById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<ProductDetailResponseDto>.Ok(await _productService.GetByIdAsync(id, ct)));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<ProductListItemResponseDto>>>> GetPaged([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<ProductListItemResponseDto>>.Ok(await _productService.GetPagedAsync(request, ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDetailResponseDto>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<ProductDetailResponseDto>.Ok(await _productService.GetByIdAsync(id, ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateProductRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await _productService.CreateAsync(request, ct)));

    [HttpPost("{productId:guid}/pricing/adjust")]
    public async Task<ActionResult<ApiResponse<ProductPricingAdjustmentResultDto>>> AdjustPricing(Guid productId, [FromBody] AdjustProductPricingRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<ProductPricingAdjustmentResultDto>.Ok(await _productService.AdjustPricingAsync(productId, request, ct)));
}
