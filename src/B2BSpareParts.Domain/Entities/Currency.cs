using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class Currency : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public int DecimalPlaces { get; set; } = 2;
    public bool IsActive { get; set; } = true;
}
