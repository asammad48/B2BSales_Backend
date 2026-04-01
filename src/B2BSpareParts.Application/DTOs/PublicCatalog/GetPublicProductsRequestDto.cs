using B2BSpareParts.Application.Common;

namespace B2BSpareParts.Application.DTOs.PublicCatalog;

public class GetPublicProductsRequestDto : PageRequest
{
    public new string? Search { get; set; }

    // Legacy single-value filters (kept for backward compatibility)
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public Guid? PartTypeId { get; set; }

    // Multi-value filters
    public List<Guid>? CategoryIds { get; set; }
    public List<Guid>? BrandIds { get; set; }
    public List<Guid>? ModelIds { get; set; }
    public List<Guid>? PartTypeIds { get; set; }

    public Guid? ShopId { get; set; }

    public IReadOnlyCollection<Guid> GetCategoryFilterIds() => MergeFilterIds(CategoryIds, CategoryId);
    public IReadOnlyCollection<Guid> GetBrandFilterIds() => MergeFilterIds(BrandIds, BrandId);
    public IReadOnlyCollection<Guid> GetModelFilterIds() => MergeFilterIds(ModelIds, ModelId);
    public IReadOnlyCollection<Guid> GetPartTypeFilterIds() => MergeFilterIds(PartTypeIds, PartTypeId);

    private static IReadOnlyCollection<Guid> MergeFilterIds(IEnumerable<Guid>? ids, Guid? singleId)
    {
        var merged = new HashSet<Guid>();

        if (ids != null)
        {
            foreach (var id in ids)
            {
                if (id != Guid.Empty)
                    merged.Add(id);
            }
        }

        if (singleId.HasValue && singleId.Value != Guid.Empty)
            merged.Add(singleId.Value);

        return merged;
    }
}
