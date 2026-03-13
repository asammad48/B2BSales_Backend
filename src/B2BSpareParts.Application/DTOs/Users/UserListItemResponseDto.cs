namespace B2BSpareParts.Application.DTOs.Users;

public class UserListItemResponseDto
{
    public Guid Id { get; set; }
    public Guid? ShopId { get; set; }
    public string? ShopName { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
    public bool IsActive { get; set; }
}
