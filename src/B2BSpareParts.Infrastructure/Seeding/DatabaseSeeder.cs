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

        //var english = new Language { Code = "en", Name = "English" };
        //var urdu = new Language { Code = "ur", Name = "Urdu", IsRtl = true };
        //var euro = new Currency { Code = "EUR", Name = "Euro", Symbol = "€" };
        //var yuan = new Currency { Code = "CNY", Name = "Chinese Yuan", Symbol = "¥" };
        //var pkr = new Currency { Code = "PKR", Name = "Pakistani Rupee", Symbol = "₨" };

        //_db.Languages.AddRange(english, urdu);
        //_db.Currencies.AddRange(euro, yuan, pkr);
        //await _db.SaveChangesAsync(ct);
        var euro = new Currency { Code = "EUR", Name = "Euro", Symbol = "€" };
        var gbp = new Currency { Code = "GBP", Name = "British Pound", Symbol = "£" };
        var chf = new Currency { Code = "CHF", Name = "Swiss Franc", Symbol = "CHF" };
        var sek = new Currency { Code = "SEK", Name = "Swedish Krona", Symbol = "kr" };
        var nok = new Currency { Code = "NOK", Name = "Norwegian Krone", Symbol = "kr" };
        var dkk = new Currency { Code = "DKK", Name = "Danish Krone", Symbol = "kr" };
        var pln = new Currency { Code = "PLN", Name = "Polish Zloty", Symbol = "zł" };
        var czk = new Currency { Code = "CZK", Name = "Czech Koruna", Symbol = "Kč" };
        var huf = new Currency { Code = "HUF", Name = "Hungarian Forint", Symbol = "Ft" };
        var ron = new Currency { Code = "RON", Name = "Romanian Leu", Symbol = "lei" };
        var yuan = new Currency { Code = "CNY", Name = "Chinese Yuan", Symbol = "¥" };

        _db.Currencies.AddRange(
            euro, gbp, chf, sek, nok, dkk, pln, czk, huf, ron, yuan
        );

        var spanish = new Language { Code = "es", Name = "Spanish" }; // Official nationwide
        var catalan = new Language { Code = "ca", Name = "Catalan" };
        var basque = new Language { Code = "eu", Name = "Basque" };
        var galician = new Language { Code = "gl", Name = "Galician" };
        var valencian = new Language { Code = "val", Name = "Valencian" }; // variant of Catalan
        var english = new Language { Code = "en", Name = "English" };
        _db.Languages.AddRange(
            spanish, catalan, basque, galician, valencian,english
        );

        await _db.SaveChangesAsync(ct);

        var tenant = new Tenant
        {
            Name = "Mobia2Z",
            Code = ApiConstants.DefaultTenantCode,
            DefaultLanguageId = english.Id,
            DefaultSellingCurrencyId = euro.Id
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        var madridShop = new Shop
        {
            TenantId = tenant.Id,
            Name = "Madrid Central Warehouse",
            Code = "MAD",
            IsMain = true,
            Address = "Calle de Alcalá 45, 28014 Madrid, Spain"
        };

        var barcelonaShop = new Shop
        {
            TenantId = tenant.Id,
            Name = "Barcelona Branch",
            Code = "BCN",
            Address = "Avinguda Diagonal 640, 08017 Barcelona, Spain"
        };

        _db.Shops.AddRange(madridShop, barcelonaShop);

        var owner = new AppUser { TenantId = tenant.Id, FullName = "Owner User", Email = "owner@demo.local", Role = UserRoles.Owner, ShopId = madridShop.Id };
        owner.PasswordHash = hasher.HashPassword(owner, "Admin123!");
        var staff = new AppUser { TenantId = tenant.Id, FullName = "Staff User", Email = "staff@demo.local", Role = UserRoles.Staff, ShopId = madridShop.Id };
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

        //var categoryDevice = new Category { TenantId = tenant.Id, Code = "DEVICE", Name = "Device" };
        //var categoryPart = new Category { TenantId = tenant.Id, Code = "SPARE", Name = "Spare Part" };
        //var categoryAccessory = new Category { TenantId = tenant.Id, Code = "ACCESSORY", Name = "Accessory" };
        //_db.Categories.AddRange(categoryDevice, categoryPart, categoryAccessory);

        //var apple = new Brand { TenantId = tenant.Id, Code = "APPLE", Name = "Apple" };
        //var samsung = new Brand { TenantId = tenant.Id, Code = "SAMSUNG", Name = "Samsung" };
        //_db.Brands.AddRange(apple, samsung);
        //await _db.SaveChangesAsync(ct);

        //var iphone13 = new DeviceModel { TenantId = tenant.Id, BrandId = apple.Id, Code = "IP13", Name = "iPhone 13" };
        //var a50 = new DeviceModel { TenantId = tenant.Id, BrandId = samsung.Id, Code = "A50", Name = "Galaxy A50" };
        //_db.DeviceModels.AddRange(iphone13, a50);

        //var screenType = new PartType { TenantId = tenant.Id, Code = "SCREEN", Name = "Screen" };
        //var batteryType = new PartType { TenantId = tenant.Id, Code = "BATTERY", Name = "Battery" };
        //var deviceType = new PartType { TenantId = tenant.Id, Code = "DEVICE", Name = "Device" };
        //_db.PartTypes.AddRange(screenType, batteryType, deviceType);
        //await _db.SaveChangesAsync(ct);

        //var iphoneProduct = new Product
        //{
        //    TenantId = tenant.Id,
        //    CategoryId = categoryDevice.Id,
        //    BrandId = apple.Id,
        //    ModelId = iphone13.Id,
        //    PartTypeId = deviceType.Id,
        //    Sku = "APL-IP13-128-BLU",
        //    Barcode = "IP13-128-BLU",
        //    Name = "Apple iPhone 13 128GB Blue PTA",
        //    TrackingType = TrackingType.Serializado,
        //    QualityType = QualityType.Original,
        //    DefaultBuyingPrice = 520m,
        //    DefaultSellingPrice = 585m,
        //    DefaultPricingMode = PricingMode.Direct,
        //    WarrantyDays = 7,
        //    ShortDescription = "Original used device",
        //    LongDescription = "A clean serialized device product for demo purposes.",
        //    Specifications = "128GB / Blue / PTA Approved",
        //    LowStockThreshold = 2,
        //    IsFeatured = true,
        //    IsPublicVisible = true,
        //    BaseCurrencyId = euro.Id
        //};
        //var a50Screen = new Product
        //{
        //    TenantId = tenant.Id,
        //    CategoryId = categoryPart.Id,
        //    BrandId = samsung.Id,
        //    ModelId = a50.Id,
        //    PartTypeId = screenType.Id,
        //    Sku = "SMS-A50-OLED",
        //    Barcode = "SMS-A50-OLED",
        //    Name = "Samsung A50 OLED Screen",
        //    TrackingType = TrackingType.PorCantidad,
        //    QualityType = QualityType.Oem,
        //    DefaultBuyingPrice = 24m,
        //    DefaultSellingPrice = 32m,
        //    DefaultPricingMode = PricingMode.PercentageBased,
        //    DefaultMarkupPercentage = 33.33m,
        //    WarrantyDays = 7,
        //    ShortDescription = "OLED screen for Samsung A50",
        //    LongDescription = "Demo OLED screen product with quantity stock.",
        //    Specifications = "OEM / Black / 7 day checking warranty",
        //    LowStockThreshold = 10,
        //    IsPublicVisible = true,
        //    BaseCurrencyId = euro.Id
        //};
        //var adhesive = new Product
        //{
        //    TenantId = tenant.Id,
        //    CategoryId = categoryAccessory.Id,
        //    BrandId = samsung.Id,
        //    ModelId = a50.Id,
        //    PartTypeId = screenType.Id,
        //    Sku = "SMS-A50-ADH",
        //    Barcode = "SMS-A50-ADH",
        //    Name = "Samsung A50 Screen Adhesive",
        //    TrackingType = TrackingType.PorCantidad,
        //    QualityType = QualityType.Oem,
        //    DefaultBuyingPrice = 1.5m,
        //    DefaultSellingPrice = 3m,
        //    DefaultPricingMode = PricingMode.Direct,
        //    WarrantyDays = 0,
        //    ShortDescription = "Adhesive for A50 screen fitting",
        //    LongDescription = "Demo accessory product.",
        //    Specifications = "Single strip adhesive",
        //    LowStockThreshold = 20,
        //    IsPublicVisible = true,
        //    BaseCurrencyId = euro.Id
        //};

        //_db.Products.AddRange(iphoneProduct, a50Screen, adhesive);
        //await _db.SaveChangesAsync(ct);

        //_db.ProductImages.AddRange(
        //    new ProductImage { TenantId = tenant.Id, ProductId = iphoneProduct.Id, FilePath = "/seed/iphone13-main.jpg", IsPrimary = true, SortOrder = 1 },
        //    new ProductImage { TenantId = tenant.Id, ProductId = a50Screen.Id, FilePath = "/seed/a50-screen-main.jpg", IsPrimary = true, SortOrder = 1 },
        //    new ProductImage { TenantId = tenant.Id, ProductId = adhesive.Id, FilePath = "/seed/a50-adhesive-main.jpg", IsPrimary = true, SortOrder = 1 }
        //);

        //_db.ProductRelations.Add(new ProductRelation
        //{
        //    TenantId = tenant.Id,
        //    ProductId = a50Screen.Id,
        //    RelatedProductId = adhesive.Id,
        //    RelationType = ProductRelationType.Accessory,
        //    SortOrder = 1
        //});

        //_db.ShopInventories.AddRange(
        //    new ShopInventory { TenantId = tenant.Id, ShopId = mainShop.Id, ProductId = a50Screen.Id, QuantityOnHand = 25, ReservedQuantity = 0, LowStockThreshold = 10 },
        //    new ShopInventory { TenantId = tenant.Id, ShopId = hallRoad.Id, ProductId = a50Screen.Id, QuantityOnHand = 8, ReservedQuantity = 0, LowStockThreshold = 10 },
        //    new ShopInventory { TenantId = tenant.Id, ShopId = mainShop.Id, ProductId = adhesive.Id, QuantityOnHand = 45, ReservedQuantity = 0, LowStockThreshold = 20 }
        //);

        //_db.SerializedInventoryUnits.AddRange(
        //    new SerializedInventoryUnit
        //    {
        //        TenantId = tenant.Id,
        //        ShopId = mainShop.Id,
        //        ProductId = iphoneProduct.Id,
        //        UnitBarcode = "SER-IP13-001",
        //        SerialNumber = "SN-IP13-001",
        //        Imei1 = "111111111111111",
        //        PurchaseCost = 520m,
        //        SalePrice = 585m
        //    },
        //    new SerializedInventoryUnit
        //    {
        //        TenantId = tenant.Id,
        //        ShopId = hallRoad.Id,
        //        ProductId = iphoneProduct.Id,
        //        UnitBarcode = "SER-IP13-002",
        //        SerialNumber = "SN-IP13-002",
        //        Imei1 = "222222222222222",
        //        PurchaseCost = 525m,
        //        SalePrice = 590m
        //    }
        //);

        _db.ExchangeRates.AddRange(
    // Base
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = euro.Id, ToCurrencyId = euro.Id, Rate = 1m, EffectiveDate = DateTime.UtcNow.Date },

    // Major
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = gbp.Id, ToCurrencyId = euro.Id, Rate = 1.15m, EffectiveDate = DateTime.UtcNow.Date }, // GBP → EUR
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = chf.Id, ToCurrencyId = euro.Id, Rate = 1.10m, EffectiveDate = DateTime.UtcNow.Date }, // CHF → EUR

    // Nordic
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = sek.Id, ToCurrencyId = euro.Id, Rate = 0.094m, EffectiveDate = DateTime.UtcNow.Date }, // SEK → EUR
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = nok.Id, ToCurrencyId = euro.Id, Rate = 0.089m, EffectiveDate = DateTime.UtcNow.Date }, // NOK → EUR
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = dkk.Id, ToCurrencyId = euro.Id, Rate = 0.134m, EffectiveDate = DateTime.UtcNow.Date }, // DKK → EUR

    // Eastern Europe
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = pln.Id, ToCurrencyId = euro.Id, Rate = 0.237m, EffectiveDate = DateTime.UtcNow.Date }, // PLN → EUR
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = czk.Id, ToCurrencyId = euro.Id, Rate = 0.041m, EffectiveDate = DateTime.UtcNow.Date }, // CZK → EUR
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = huf.Id, ToCurrencyId = euro.Id, Rate = 0.0026m, EffectiveDate = DateTime.UtcNow.Date }, // HUF → EUR
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = ron.Id, ToCurrencyId = euro.Id, Rate = 0.196m, EffectiveDate = DateTime.UtcNow.Date }, // RON → EUR

    // Asia
    new ExchangeRate { TenantId = tenant.Id, FromCurrencyId = yuan.Id, ToCurrencyId = euro.Id, Rate = 0.128m, EffectiveDate = DateTime.UtcNow.Date }
);

        _db.Notifications.Add(new Notification
        {
            TenantId = tenant.Id,
            Type = NotificationType.General,
            Title = "Seed complete",
            Message = "Intial data has been created."
        });

        await _db.SaveChangesAsync(ct);
    }
}
