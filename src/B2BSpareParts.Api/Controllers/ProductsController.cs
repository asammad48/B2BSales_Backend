using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
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
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Products.ProductListItemResponseDto>>>> GetPublic([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Products.ProductListItemResponseDto>>.Ok(await _productService.GetPagedAsync(request, ct)));

    [AllowAnonymous]
    [HttpGet("public/{id:guid}")]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.DTOs.Products.ProductDetailResponseDto>>> GetPublicById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.DTOs.Products.ProductDetailResponseDto>.Ok(await _productService.GetByIdAsync(id, ct)));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Products.ProductListItemResponseDto>>>> GetPaged([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Products.ProductListItemResponseDto>>.Ok(await _productService.GetPagedAsync(request, ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.DTOs.Products.ProductDetailResponseDto>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.DTOs.Products.ProductDetailResponseDto>.Ok(await _productService.GetByIdAsync(id, ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] B2BSpareParts.Application.DTOs.Products.CreateProductRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await _productService.CreateAsync(request, ct)));
}
