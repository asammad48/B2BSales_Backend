using B2BSpareParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<DeviceModel> DeviceModels => Set<DeviceModel>();
    public DbSet<PartType> PartTypes => Set<PartType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductRelation> ProductRelations => Set<ProductRelation>();
    public DbSet<ProductCompatibleModel> ProductCompatibleModels => Set<ProductCompatibleModel>();
    public DbSet<ShopInventory> ShopInventories => Set<ShopInventory>();
    public DbSet<SerializedInventoryUnit> SerializedInventoryUnits => Set<SerializedInventoryUnit>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ThemeSetting> ThemeSettings => Set<ThemeSetting>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Shop>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();
        modelBuilder.Entity<ShopInventory>().HasIndex(x => new { x.TenantId, x.ShopId, x.ProductId }).IsUnique();
        modelBuilder.Entity<SerializedInventoryUnit>().HasIndex(x => new { x.TenantId, x.UnitBarcode }).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(x => new { x.TenantId, x.OrderNumber }).IsUnique();
        modelBuilder.Entity<Currency>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Language>().HasIndex(x => x.Code).IsUnique();

        modelBuilder.Entity<ProductRelation>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductRelation>()
            .HasOne(x => x.RelatedProduct)
            .WithMany()
            .HasForeignKey(x => x.RelatedProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransferItem>()
            .HasOne(x => x.StockTransfer)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.StockTransferId);

        modelBuilder.Entity<StockTransfer>()
            .HasOne(x => x.SourceShop)
            .WithMany()
            .HasForeignKey(x => x.SourceShopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransfer>()
            .HasOne(x => x.DestinationShop)
            .WithMany()
            .HasForeignKey(x => x.DestinationShopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
