namespace B2BSpareParts.Application.DTOs;

public class DateRangeReportRequestDto
{
    public RangeType RangeType { get; set; } = RangeType.Day;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
