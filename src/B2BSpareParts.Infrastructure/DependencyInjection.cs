using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Infrastructure.Auth;
using B2BSpareParts.Infrastructure.Persistence;
using B2BSpareParts.Infrastructure.Seeding;
using B2BSpareParts.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace B2BSpareParts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, CurrentTenantContext>();
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<DatabaseSeeder>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPublicCatalogService, PublicCatalogService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IThemeService, ThemeService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
