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
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public ProductsController(IProductService productService, IConfiguration configuration, IWebHostEnvironment env)
    {
        _productService = productService;
        _configuration = configuration;
        _env = env;
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
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromForm] CreateProductRequestDto request, [FromForm] List<IFormFile> images, CancellationToken ct)
    {
        var uploadFolder = _configuration["FileStorage:UploadFolder"] ?? "uploads";
        var uploadPath = Path.Combine(_env.ContentRootPath, uploadFolder);

        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        for (int i = 0; i < images.Count; i++)
        {
            var file = images[i];
            if (file.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream, ct);
                }

                request.Images ??= [];

                // If images were already provided in the DTO, update the first matching one or add a new one
                if (request.Images.Count > i)
                {
                    request.Images[i].FilePath = $"{uploadFolder}/{fileName}";
                }
                else
                {
                    request.Images.Add(new CreateProductImageRequestDto
                    {
                        FilePath = $"{uploadFolder}/{fileName}",
                        IsPrimary = i == 0,
                        SortOrder = i
                    });
                }
            }
        }

        return Ok(ApiResponse<Guid>.Ok(await _productService.CreateAsync(request, ct)));
    }

    [HttpPost("{productId:guid}/pricing/adjust")]
    public async Task<ActionResult<ApiResponse<ProductPricingAdjustmentResultDto>>> AdjustPricing(Guid productId, [FromBody] AdjustProductPricingRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<ProductPricingAdjustmentResultDto>.Ok(await _productService.AdjustPricingAsync(productId, request, ct)));
}
