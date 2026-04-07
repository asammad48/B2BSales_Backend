using System.ComponentModel.DataAnnotations;
using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Clients;

public class CreateClientRequestDto
{
    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string BusinessName { get; set; } = default!;

    [Required]
    public string Phone { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = default!;

    public string? Address { get; set; }

    public Guid? PreferredCurrencyId { get; set; }

    public Guid? PreferredLanguageId { get; set; }

    public Guid? PriceTierId { get; set; }

    public bool IsActive { get; set; } = true;

    public ClientStatus Status { get; set; } = ClientStatus.Approved;
}
