namespace B2BSpareParts.Application.DTOs.Clients;

public class ClientResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Status { get; set; } = default!;
}
