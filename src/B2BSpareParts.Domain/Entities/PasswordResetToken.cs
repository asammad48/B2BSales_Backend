using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class PasswordResetToken : TenantEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsUsed { get; set; }

    public AppUser? User { get; set; }
}
