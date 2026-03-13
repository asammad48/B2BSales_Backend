namespace B2BSpareParts.Application.Contracts;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid? UserId { get; }
    string? Role { get; }
}
