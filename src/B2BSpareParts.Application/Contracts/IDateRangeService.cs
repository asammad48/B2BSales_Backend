using B2BSpareParts.Application.DTOs;

namespace B2BSpareParts.Application.Contracts;

public interface IDateRangeService
{
    (DateTime start, DateTime end) CalculateDateRange(RangeType rangeType, DateTime? startDate, DateTime? endDate);
}
