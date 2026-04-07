using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IWebHostEnvironment _env;
    private readonly FileStorageOptions _fileStorageOptions;

    public ProductsController(IProductService productService, IOptions<FileStorageOptions> fileStorageOptions, IWebHostEnvironment env)
    {
        _productService = productService;
        _fileStorageOptions = fileStorageOptions.Value;
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
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromForm] CreateProductRequestDto request, [FromForm] List<IFormFile> imageFiles, CancellationToken ct)
    {
        var configuredStoragePath = _fileStorageOptions.StoragePath
            ?? _fileStorageOptions.UploadFolder
            ?? "uploads";
        var uploadPath = Path.IsPathRooted(configuredStoragePath)
            ? configuredStoragePath
            : Path.GetFullPath(Path.Combine(_env.ContentRootPath, configuredStoragePath));

        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        request.Images ??= [];

        for (int i = 0; i < imageFiles.Count; i++)
        {
            var file = imageFiles[i];
            if (file.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream, ct);
                }

                // If images were already provided in the DTO, update the first matching one or add a new one
                if (request.Images.Count > i)
                {
                    request.Images[i].FilePath = fileName;
                }
                else
                {
                    request.Images.Add(new CreateProductImageRequestDto
                    {
                        FilePath = fileName,
                        IsPrimary = request.Images.Count == 0, // Fallback if no image is marked primary
                        SortOrder = request.Images.Count
                    });
                }
            }
        }

        return Ok(ApiResponse<Guid>.Ok(await _productService.CreateAsync(request, ct)));
    }

    [HttpPost("{productId:guid}/pricing/adjust")]
    public async Task<ActionResult<ApiResponse<ProductPricingAdjustmentResultDto>>> AdjustPricing(Guid productId, [FromBody] AdjustProductPricingRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<ProductPricingAdjustmentResultDto>.Ok(await _productService.AdjustPricingAsync(productId, request, ct)));

    [HttpPatch("{productId:guid}/flags")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateFlags(Guid productId, [FromBody] UpdateProductFlagsRequestDto request, CancellationToken ct)
    {
        await _productService.UpdateFlagsAsync(productId, request, ct);
        return Ok(ApiResponse<object>.Ok(new { Message = "Product flags updated successfully." }));
    }
}
