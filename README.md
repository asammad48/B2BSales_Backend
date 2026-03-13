# B2B Spare Parts Backend (.NET + PostgreSQL)

This package contains a **clean architecture backend starter** for the B2B mobile spare parts platform discussed in chat.

## Important honesty
I could not compile or run this inside the current environment because the **.NET SDK is not installed here**. So I generated the code carefully, but you should expect to do a first compile pass and fix any small compile issues if your local SDK/package versions differ.

## Included
- Clean architecture style split:
  - `B2BSpareParts.Domain`
  - `B2BSpareParts.Application`
  - `B2BSpareParts.Infrastructure`
  - `B2BSpareParts.Api`
  - `B2BSpareParts.Common`
  - `B2BSpareParts.Scraper.Console`
- PostgreSQL with EF Core
- Seeded tenant, shops, users, currencies, languages, brands, models, products, inventory, client
- JWT auth
- String enums in API for better NSwag generation
- Controllers for:
  - Auth
  - Lookups
  - Products
  - Inventory
  - Orders
  - Users
  - Themes
  - Notifications
- Basic password reset flow
- Stock adjustment
- Stock transfer
- Low stock notifications
- Guest/public product listing with hidden prices logic support through `isGuestView`

## Seed users
- `owner@demo.local` / `Admin123!`
- `staff@demo.local` / `Staff123!`
- `client@demo.local` / `Client123!`

## Recommended local steps
1. Install .NET 8 SDK or adjust target frameworks to your preferred version.
2. Create PostgreSQL database.
3. Update connection string in `B2BSpareParts.Api/appsettings.Development.json`
4. Run:
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run --project B2BSpareParts.Api`
5. Open Swagger and verify endpoints.
6. Generate NSwag client from swagger.

## Notes
- This is a **strong starter backend**, not a 100% exhaustive enterprise backend.
- I intentionally kept inventory logic **practical/basic**:
  - quantity-based stock
  - serialized stock units
  - stock in
  - stock adjustment
  - stock transfer
  - low stock notifications
  - order stock deduction
- For production, add:
  - migrations
  - refresh tokens
  - email/SMS sender
  - audit logging middleware
  - better permissions matrix
  - background jobs
  - integration tests

## NSwag
A `nswag.json` file is included in the API project.

## Latest locked business rule
- Stock is deducted **only** on pickup confirmation / payment completion.
- If stock is no longer available at completion time, the order is moved to `UnableToFulfill`.
- No stock is reserved at order placement in this version.


## DTO Structure
Each DTO is now placed in its own file under a service/module folder. Example:
- `Application/DTOs/Products/CreateProductRequestDto.cs`
- `Application/DTOs/Inventory/StockInRequestDto.cs`
- `Application/DTOs/Auth/LoginResponseDto.cs`

All DTO class names end with the `Dto` suffix.

## Public Website Support
Public storefront endpoints are included for a non-logged-in website:
- `GET /api/public/storefront/theme`
- `GET /api/public/storefront/products`
- `GET /api/public/storefront/products/{id}`

For guests:
- prices are locked
- `CanOrder = false`
- `IsPriceLocked = true`
- product detail includes `AvailabilityMessage` telling the frontend that login is required to view prices and place orders

Authenticated product endpoints remain available under `/api/Products`.
