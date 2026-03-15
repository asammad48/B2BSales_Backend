namespace B2BSpareParts.Application.DTOs.Orders.ClientOrders;

public class ClientOrderSummaryDto
{
    public Guid ClientId { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ReadyForPickupOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int UnableToFulfillOrders { get; set; }
}
