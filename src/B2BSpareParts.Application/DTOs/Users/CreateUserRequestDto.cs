using System.ComponentModel.DataAnnotations;

namespace B2BSpareParts.Application.DTOs.Users;

public class CreateUserRequestDto
{
    [Required]
    public string FullName { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    public string? Phone { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = default!;

    [Required]
    public string Role { get; set; } = default!;

    public Guid? ShopId { get; set; }

    public Guid? PreferredLanguageId { get; set; }

    public bool IsActive { get; set; } = true;
}
