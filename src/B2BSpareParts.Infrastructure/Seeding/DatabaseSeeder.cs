using B2BSpareParts.Common;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Seeding;

public class DatabaseSeeder
{
    private readonly AppDbContext _db;

    public DatabaseSeeder(AppDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Tenants.AnyAsync(ct))
            return;

        var hasher = new PasswordHasher<AppUser>();

        var english = new Language { Code = "en", Name = "English" };
        var urdu = new Language { Code = "ur", Name = "Urdu", IsRtl = true };
        var euro = new Currency { Code = "EUR", Name = "Euro", Symbol = "€" };
        var yuan = new Currency { Code = "CNY", Name = "Chinese Yuan", Symbol = "¥" };
        var pkr = new Currency { Code = "PKR", Name = "Pakistani Rupee", Symbol = "₨" };

        _db.Languages.AddRange(english, urdu);
        _db.Currencies.AddRange(euro, yuan, pkr);
        await _db.SaveChangesAsync(ct);

        var tenant = new Tenant
        {
            Name = "Demo Mobile Parts",
            Code = ApiConstants.DefaultTenantCode,
            BaseCurrencyId = yuan.Id,
            DefaultLanguageId = english.Id
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        var mainShop = new Shop { TenantId = tenant.Id, Name = "Main Warehouse", Code = "MAIN", IsMain = true, Address = "Rawalpindi" };
        var hallRoad = new Shop { TenantId = tenant.Id, Name = "Hall Road Branch", Code = "HALL", Address = "Lahore" };
        _db.Shops.AddRange(mainShop, hallRoad);

        var owner = new AppUser { TenantId = tenant.Id, FullName = "Owner User", Email = "owner@demo.local", Role = UserRoles.Owner, ShopId = mainShop.Id };
        owner.PasswordHash = hasher.HashPassword(owner, "Admin123!");
        var staff = new AppUser { TenantId = tenant.Id, FullName = "Staff User", Email = "staff@demo.local", Role = UserRoles.Staff, ShopId = mainShop.Id };
        staff.PasswordHash = hasher.HashPassword(staff, "Staff123!");
        var clientUser = new AppUser { TenantId = tenant.Id, FullName = "Client User", Email = "client@demo.local", Role = UserRoles.Client };
        clientUser.PasswordHash = hasher.HashPassword(clientUser, "Client123!");
        _db.Users.AddRange(owner, staff, clientUser);

        var client = new Client
        {
            TenantId = tenant.Id,
            Name = "Mujtaba",
            BusinessName = "Star Mobile Repair",
            Email = "client@demo.local",
            Phone = "03000000000",
            Status = ClientStatus.Approved,
            PreferredCurrencyId = euro.Id,
            PreferredLanguageId = english.Id
        };
        _db.Clients.Add(client);

        var theme = new ThemeSetting
        {
            TenantId = tenant.Id,
            PrimaryColor = "#111827",
            SecondaryColor = "#F59E0B",
            AccentColor = "#2563EB",
            FooterText = "Demo Mobile Parts"
        };
        _db.ThemeSettings.Add(theme);

        var categoryDevice = new Category { TenantId = tenant.Id, Code = "DEVICE", Name = "Device" };
        var categoryPart = new Category { TenantId = tenant.Id, Code = "SPARE", Name = "Spare Part" };
        var categoryAccessory = new Category { TenantId = tenant.Id, Code = "ACCESSORY", Name = "Accessory" };
        _db.Categories.AddRange(categoryDevice, categoryPart, categoryAccessory);

        var apple = new Brand { TenantId = tenant.Id, Code = "APPLE", Name = "Apple" };
        var samsung = new Brand { TenantId = tenant.Id, Code = "SAMSUNG", Name = "Samsung" };
        _db.Brands.AddRange(apple, samsung);
        await _db.SaveChangesAsync(ct);

        var iphone13 = new DeviceModel { TenantId = tenant.Id, BrandId = apple.Id, Code = "IP13", Name = "iPhone 13" };
        var a50 = new DeviceModel { TenantId = tenant.Id, BrandId = samsung.Id, Code = "A50", Name = "Galaxy A50" };
        _db.DeviceModels.AddRange(iphone13, a50);

        var screenType = new PartType { TenantId = tenant.Id, Code = "SCREEN", Name = "Screen" };
        var batteryType = new PartType { TenantId = tenant.Id, Code = "BATTERY", Name = "Battery" };
        var deviceType = new PartType { TenantId = tenant.Id, Code = "DEVICE", Name = "Device" };
        _db.PartTypes.AddRange(screenType, batteryType, deviceType);
        await _db.SaveChangesAsync(ct);

        var iphoneProduct = new Product
        {
            TenantId = tenant.Id,
            CategoryId = categoryDevice.Id,
            BrandId = apple.Id,
            ModelId = iphone13.Id,
            PartTypeId = deviceType.Id,
            Sku = "APL-IP13-128-BLU",
            Barcode = "IP13-128-BLU",
            Name = "Apple iPhone 13 128GB Blue PTA",
            TrackingType = TrackingType.Serialized,
            QualityType = QualityType.Original,
            DefaultBuyingPrice = 520m,
            DefaultSellingPrice = 585m,
            DefaultPricingMode = PricingMode.Direct,
            WarrantyDays = 7,
            ShortDescription = "Original used device",
            LongDescription = "A clean serialized device product for demo purposes.",
            Specifications = "128GB / Blue / PTA Approved",
            LowStockThreshold = 2,
            IsFeatured = true,
            IsPublicVisible = true
        };
        var a50Screen = new Product
        {
            TenantId = tenant.Id,
            CategoryId = categoryPart.Id,
            BrandId = samsung.Id,
            ModelId = a50.Id,
            PartTypeId = screenType.Id,
            Sku = "SMS-A50-OLED",
            Barcode = "SMS-A50-OLED",
            Name = "Samsung A50 OLED Screen",
            TrackingType = TrackingType.QuantityBased,
            QualityType = QualityType.OEM,
            DefaultBuyingPrice = 24m,
            DefaultSellingPrice = 32m,
            DefaultPricingMode = PricingMode.PercentageBased,
            DefaultMarkupPercentage = 33.33m,
            WarrantyDays = 7,
            ShortDescription = "OLED screen for Samsung A50",
            LongDescription = "Demo OLED screen product with quantity stock.",
            Specifications = "OEM / Black / 7 day checking warranty",
            LowStockThreshold = 10,
            IsPublicVisible = true
        };
        var adhesive = new Product
        {
            TenantId = tenant.Id,
            CategoryId = categoryAccessory.Id,
            BrandId = samsung.Id,
            ModelId = a50.Id,
            PartTypeId = screenType.Id,
            Sku = "SMS-A50-ADH",
            Barcode = "SMS-A50-ADH",
            Name = "Samsung A50 Screen Adhesive",
            TrackingType = TrackingType.QuantityBased,
            QualityType = QualityType.OEM,
            DefaultBuyingPrice = 1.5m,
            DefaultSellingPrice = 3m,
            DefaultPricingMode = PricingMode.Direct,
            WarrantyDays = 0,
            ShortDescription = "Adhesive for A50 screen fitting",
            LongDescription = "Demo accessory product.",
            Specifications = "Single strip adhesive",
            LowStockThreshold = 20,
            IsPublicVisible = true
        };

        _db.Products.AddRange(iphoneProduct, a50Screen, adhesive);
        await _db.SaveChangesAsync(ct);

        _db.ProductImages.AddRange(
            new ProductImage { TenantId = tenant.Id, ProductId = iphoneProduct.Id, FilePath = "/seed/iphone13-main.jpg", IsPrimary = true, SortOrder = 1 },
            new ProductImage { TenantId = tenant.Id, ProductId = a50Screen.Id, FilePath = "/seed/a50-screen-main.jpg", IsPrimary = true, SortOrder = 1 },
            new ProductImage { TenantId = tenant.Id, ProductId = adhesive.Id, FilePath = "/seed/a50-adhesive-main.jpg", IsPrimary = true, SortOrder = 1 }
        );

        _db.ProductRelations.Add(new ProductRelation
        {
            TenantId = tenant.Id,
            ProductId = a50Screen.Id,
            RelatedProductId = adhesive.Id,
            RelationType = ProductRelationType.Accessory,
            SortOrder = 1
        });

        _db.ShopInventories.AddRange(
            new ShopInventory { TenantId = tenant.Id, ShopId = mainShop.Id, ProductId = a50Screen.Id, QuantityOnHand = 25, ReservedQuantity = 0, LowStockThreshold = 10 },
            new ShopInventory { TenantId = tenant.Id, ShopId = hallRoad.Id, ProductId = a50Screen.Id, QuantityOnHand = 8, ReservedQuantity = 0, LowStockThreshold = 10 },
            new ShopInventory { TenantId = tenant.Id, ShopId = mainShop.Id, ProductId = adhesive.Id, QuantityOnHand = 45, ReservedQuantity = 0, LowStockThreshold = 20 }
        );

        _db.SerializedInventoryUnits.AddRange(
            new SerializedInventoryUnit
            {
                TenantId = tenant.Id,
                ShopId = mainShop.Id,
                ProductId = iphoneProduct.Id,
                UnitBarcode = "SER-IP13-001",
                SerialNumber = "SN-IP13-001",
                Imei1 = "111111111111111",
                PurchaseCost = 520m,
                SalePrice = 585m
            },
            new SerializedInventoryUnit
            {
                TenantId = tenant.Id,
                ShopId = hallRoad.Id,
                ProductId = iphoneProduct.Id,
                UnitBarcode = "SER-IP13-002",
                SerialNumber = "SN-IP13-002",
                Imei1 = "222222222222222",
                PurchaseCost = 525m,
                SalePrice = 590m
            }
        );

        _db.ExchangeRates.AddRange(
            new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = euro.Id, ToCurrencyId = yuan.Id, Rate = 7.8m, EffectiveDate = DateTime.UtcNow.Date },
            new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = pkr.Id, ToCurrencyId = yuan.Id, Rate = 0.025m, EffectiveDate = DateTime.UtcNow.Date }
        );

        _db.Notifications.Add(new Notification
        {
            TenantId = tenant.Id,
            Type = NotificationType.General,
            Title = "Seed complete",
            Message = "Demo seed data has been created."
        });

        await _db.SaveChangesAsync(ct);
    }
}
