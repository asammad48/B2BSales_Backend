namespace B2BSpareParts.Application.DTOs.Reports;

public class OrderStatusSummaryDto
{
    public int PendingOrders { get; set; }
    public int ReadyForPickupOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int UnableToFulfillOrders { get; set; }
}
