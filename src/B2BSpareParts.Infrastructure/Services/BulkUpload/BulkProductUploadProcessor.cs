using System.Globalization;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Domain.Entities.BulkUpload;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;

namespace B2BSpareParts.Infrastructure.Services.BulkUpload;

public class BulkProductUploadProcessor
{
    private readonly AppDbContext _db;
    private readonly ILogger<BulkProductUploadProcessor> _logger;

    public BulkProductUploadProcessor(AppDbContext db, ILogger<BulkProductUploadProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.BulkUploadJobs.FirstOrDefaultAsync(x => x.Id == jobId && !x.IsDeleted, ct);
        if (job == null) return;

        if (job.Status == BulkUploadJobStatus.Processing) return;

        try
        {
            job.Status = BulkUploadJobStatus.Processing;
            job.StartedAt ??= DateTimeOffset.UtcNow;
            job.ErrorMessage = null;
            await _db.SaveChangesAsync(ct);

            var fullPath = ResolveContentPath(job.FilePath);
            if (!File.Exists(fullPath))
                throw new InvalidOperationException("Source CSV file not found for bulk job.");

            var rows = ReadRows(fullPath).ToList();
            job.TotalRows = rows.Count;
            await _db.SaveChangesAsync(ct);

            var existingItems = await _db.BulkUploadJobItems
                .Where(x => x.JobId == jobId && x.TenantId == job.TenantId && !x.IsDeleted)
                .ToDictionaryAsync(x => x.RowNumber, ct);

            var processedCompleted = existingItems.Values.Count(x => x.Status == BulkUploadRowStatus.Completed);

            var pendingRows = rows.Where(x => !existingItems.TryGetValue(x.RowNumber, out var item) || item.Status != BulkUploadRowStatus.Completed).ToList();

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var categories = await UpsertCategoriesAsync(job.TenantId, pendingRows, ct);
            var partTypes = await UpsertPartTypesAsync(job.TenantId, pendingRows, ct);
            var brands = await UpsertBrandsAsync(job.TenantId, pendingRows, ct);
            var models = await UpsertModelsAsync(job.TenantId, pendingRows, brands, ct);
            var tenant = await _db.Tenants.FirstAsync(x => x.Id == job.TenantId, ct);

            var success = 0;
            var fail = 0;
            var processed = processedCompleted;

            foreach (var row in pendingRows)
            {
                processed++;
                try
                {
                    var sku = string.IsNullOrWhiteSpace(row.Sku) ? GenerateSku(row.ProductName, row.RowNumber) : row.Sku.Trim();
                    var categoryId = categories[Normalize(row.NewCategory)];
                    var partTypeId = partTypes[Normalize(row.NewPartType)];
                    var brandId = brands[Normalize(row.NewBrand)];
                    var modelId = models[(Normalize(row.NewBrand), Normalize(row.NewModel))];

                    var existingProduct = await _db.Products.FirstOrDefaultAsync(x => x.TenantId == job.TenantId && !x.IsDeleted && x.Sku.ToLower() == sku.ToLower(), ct);
                    if (existingProduct == null)
                    {
                        existingProduct = new Product
                        {
                            TenantId = job.TenantId,
                            CategoryId = categoryId,
                            PartTypeId = partTypeId,
                            BrandId = brandId,
                            ModelId = modelId,
                            Sku = sku,
                            Name = row.ProductName,
                            ShortDescription = row.ShortDescription,
                            LongDescription = row.LongDescription,
                            Specifications = row.Specifications,
                            TrackingType = ParseTrackingType(row.TrackingType),
                            QualityType = ParseQualityType(row.QualityType),
                            BaseCurrencyId = tenant.DefaultSellingCurrencyId,
                            BasePrice = 0,
                            DefaultBuyingPrice = 0,
                            DefaultSellingPrice = 0,
                            DefaultPricingMode = PricingMode.Direct,
                            WarrantyDays = 0,
                            LowStockThreshold = 5
                        };

                        if (!string.IsNullOrWhiteSpace(row.DownloadedImageLocalPath))
                        {
                            existingProduct.Images.Add(new ProductImage
                            {
                                TenantId = job.TenantId,
                                FilePath = row.DownloadedImageLocalPath.Trim(),
                                IsPrimary = true,
                                SortOrder = 0
                            });
                        }

                        _db.Products.Add(existingProduct);
                    }

                    var item = existingItems.GetValueOrDefault(row.RowNumber);
                    if (item == null)
                    {
                        item = new BulkUploadJobItem
                        {
                            TenantId = job.TenantId,
                            JobId = jobId,
                            RowNumber = row.RowNumber
                        };
                        _db.BulkUploadJobItems.Add(item);
                        existingItems[row.RowNumber] = item;
                    }

                    item.Status = BulkUploadRowStatus.Completed;
                    item.ProductId = existingProduct.Id;
                    item.ErrorMessage = null;
                    success++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed processing row {RowNumber} for job {JobId}", row.RowNumber, jobId);
                    var item = existingItems.GetValueOrDefault(row.RowNumber);
                    if (item == null)
                    {
                        item = new BulkUploadJobItem
                        {
                            TenantId = job.TenantId,
                            JobId = jobId,
                            RowNumber = row.RowNumber
                        };
                        _db.BulkUploadJobItems.Add(item);
                        existingItems[row.RowNumber] = item;
                    }

                    item.Status = BulkUploadRowStatus.Failed;
                    item.ErrorMessage = ex.Message;
                    item.ProductId = null;
                    fail++;
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var totalCompleted = await _db.BulkUploadJobItems.CountAsync(x => x.JobId == jobId && x.Status == BulkUploadRowStatus.Completed && !x.IsDeleted, ct);
            var totalFailed = await _db.BulkUploadJobItems.CountAsync(x => x.JobId == jobId && x.Status == BulkUploadRowStatus.Failed && !x.IsDeleted, ct);

            job.ProcessedRows = totalCompleted + totalFailed;
            job.SuccessfulRows = totalCompleted;
            job.FailedRows = totalFailed;
            job.Status = totalFailed > 0 ? BulkUploadJobStatus.Failed : BulkUploadJobStatus.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            job.Status = BulkUploadJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogError(ex, "Bulk upload job {JobId} failed.", jobId);
        }
    }

    private static IEnumerable<BulkProductCsvRow> ReadRows(string fullPath)
    {
        using var parser = new TextFieldParser(fullPath)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true
        };

        parser.SetDelimiters(",");
        if (parser.EndOfData) yield break;

        var header = parser.ReadFields() ?? [];
        var map = header.Select((name, index) => new { name = name?.Trim() ?? string.Empty, index })
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

        var rowNumber = 1;
        while (!parser.EndOfData)
        {
            rowNumber++;
            var fields = parser.ReadFields() ?? [];

            string Get(string key)
            {
                if (!map.TryGetValue(key, out var idx)) return string.Empty;
                return idx < fields.Length ? fields[idx] ?? string.Empty : string.Empty;
            }

            yield return new BulkProductCsvRow
            {
                RowNumber = rowNumber,
                ProductName = Get("ProductName").Trim(),
                Sku = Get("Sku").Trim(),
                ShortDescription = Get("ShortDescription").Trim(),
                LongDescription = Get("LongDescription").Trim(),
                Specifications = Get("Specifications").Trim(),
                TrackingType = Get("TrackingType").Trim(),
                QualityType = Get("QualityType").Trim(),
                DownloadedImageLocalPath = Get("DownloadedImageLocalPath").Trim(),
                NewBrand = Get("NewBrand").Trim(),
                NewModel = Get("NewModel").Trim(),
                NewCategory = Get("NewCategory").Trim(),
                NewPartType = Get("NewPartType").Trim()
            };
        }
    }

    private async Task<Dictionary<string, Guid>> UpsertCategoriesAsync(Guid tenantId, List<BulkProductCsvRow> rows, CancellationToken ct)
    {
        var names = rows.Select(x => x.NewCategory).Where(x => !string.IsNullOrWhiteSpace(x)).Select(Normalize).Distinct().ToList();
        var existing = await _db.Categories.Where(x => x.TenantId == tenantId && !x.IsDeleted).ToListAsync(ct);
        var map = existing.ToDictionary(x => Normalize(x.Name), x => x.Id);

        foreach (var key in names)
        {
            if (!map.ContainsKey(key))
            {
                var display = rows.First(x => Normalize(x.NewCategory) == key).NewCategory.Trim();
                var entity = new Category { TenantId = tenantId, Name = display, Code = BuildCode(display, "CAT") };
                _db.Categories.Add(entity);
                map[key] = entity.Id;
            }
        }

        await _db.SaveChangesAsync(ct);
        return map;
    }

    private async Task<Dictionary<string, Guid>> UpsertPartTypesAsync(Guid tenantId, List<BulkProductCsvRow> rows, CancellationToken ct)
    {
        var names = rows.Select(x => x.NewPartType).Where(x => !string.IsNullOrWhiteSpace(x)).Select(Normalize).Distinct().ToList();
        var existing = await _db.PartTypes.Where(x => x.TenantId == tenantId && !x.IsDeleted).ToListAsync(ct);
        var map = existing.ToDictionary(x => Normalize(x.Name), x => x.Id);

        foreach (var key in names)
        {
            if (!map.ContainsKey(key))
            {
                var display = rows.First(x => Normalize(x.NewPartType) == key).NewPartType.Trim();
                var entity = new PartType { TenantId = tenantId, Name = display, Code = BuildCode(display, "PT") };
                _db.PartTypes.Add(entity);
                map[key] = entity.Id;
            }
        }

        await _db.SaveChangesAsync(ct);
        return map;
    }

    private async Task<Dictionary<string, Guid>> UpsertBrandsAsync(Guid tenantId, List<BulkProductCsvRow> rows, CancellationToken ct)
    {
        var names = rows.Select(x => x.NewBrand).Where(x => !string.IsNullOrWhiteSpace(x)).Select(Normalize).Distinct().ToList();
        var existing = await _db.Brands.Where(x => x.TenantId == tenantId && !x.IsDeleted).ToListAsync(ct);
        var map = existing.ToDictionary(x => Normalize(x.Name), x => x.Id);

        foreach (var key in names)
        {
            if (!map.ContainsKey(key))
            {
                var display = rows.First(x => Normalize(x.NewBrand) == key).NewBrand.Trim();
                var entity = new Brand { TenantId = tenantId, Name = display, Code = BuildCode(display, "BR") };
                _db.Brands.Add(entity);
                map[key] = entity.Id;
            }
        }

        await _db.SaveChangesAsync(ct);
        return map;
    }

    private async Task<Dictionary<(string Brand, string Model), Guid>> UpsertModelsAsync(Guid tenantId, List<BulkProductCsvRow> rows, Dictionary<string, Guid> brands, CancellationToken ct)
    {
        var keys = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.NewBrand) && !string.IsNullOrWhiteSpace(x.NewModel))
            .Select(x => (Brand: Normalize(x.NewBrand), Model: Normalize(x.NewModel)))
            .Distinct()
            .ToList();

        var existing = await _db.DeviceModels.Where(x => x.TenantId == tenantId && !x.IsDeleted).ToListAsync(ct);
        var map = existing.ToDictionary(x => (Brand: x.BrandId.ToString(), Model: Normalize(x.Name)), x => x.Id);
        var result = new Dictionary<(string Brand, string Model), Guid>();

        foreach (var key in keys)
        {
            var brandId = brands[key.Brand];
            var lookup = (Brand: brandId.ToString(), Model: key.Model);
            if (!map.TryGetValue(lookup, out var modelId))
            {
                var display = rows.First(x => Normalize(x.NewBrand) == key.Brand && Normalize(x.NewModel) == key.Model).NewModel.Trim();
                var entity = new DeviceModel { TenantId = tenantId, BrandId = brandId, Name = display, Code = BuildCode(display, "MDL") };
                _db.DeviceModels.Add(entity);
                modelId = entity.Id;
                map[lookup] = modelId;
            }

            result[key] = modelId;
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }

    private static string BuildCode(string input, string prefix)
    {
        var chars = new string(input.Where(char.IsLetterOrDigit).Take(8).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(chars)) chars = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"{prefix}-{chars}";
    }

    private static string GenerateSku(string productName, int rowNumber)
    {
        var slug = new string(productName.Where(char.IsLetterOrDigit).Take(8).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(slug)) slug = "ITEM";
        return $"AUTO-{slug}-{rowNumber.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static TrackingType ParseTrackingType(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "1" or "por cantidad" => TrackingType.PorCantidad,
            "2" or "serializado" => TrackingType.Serializado,
            _ => TrackingType.PorCantidad
        };
    }

    private static QualityType ParseQualityType(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "1" or "compatible" => QualityType.Compatible,
            "2" or "deji" => QualityType.Deji,
            "3" or "desconocido" => QualityType.Desconocido,
            "4" or "oem" => QualityType.Oem,
            "5" or "original" => QualityType.Original,
            "6" or "original desmontaje" => QualityType.OriginalDesmontaje,
            "7" or "service pack" => QualityType.ServicePack,
            _ => QualityType.Desconocido
        };
    }

    private sealed class BulkProductCsvRow
    {
        public int RowNumber { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string LongDescription { get; set; } = string.Empty;
        public string Specifications { get; set; } = string.Empty;
        public string TrackingType { get; set; } = string.Empty;
        public string QualityType { get; set; } = string.Empty;
        public string DownloadedImageLocalPath { get; set; } = string.Empty;
        public string NewBrand { get; set; } = string.Empty;
        public string NewModel { get; set; } = string.Empty;
        public string NewCategory { get; set; } = string.Empty;
        public string NewPartType { get; set; } = string.Empty;
    }

    private static string ResolveContentPath(string relativePath)
    {
        return Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(Directory.GetCurrentDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
