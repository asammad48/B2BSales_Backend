using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Playwright;

var options = CliOptions.Parse(args);
await Scraper.RunAsync(options);

internal sealed class CliOptions
{
    public string BaseUrl { get; set; } = "https://www.cellb2b.com";
    public string OutputDir { get; set; } = "output";
    public bool Headful { get; set; }
    public int MaxCategories { get; set; }

    public static CliOptions Parse(string[] args)
    {
        var result = new CliOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--base-url":
                    result.BaseUrl = GetValue(args, ref i, arg).TrimEnd('/');
                    break;
                case "--output-dir":
                    result.OutputDir = GetValue(args, ref i, arg);
                    break;
                case "--headful":
                    result.Headful = true;
                    break;
                case "--max-categories":
                    if (!int.TryParse(GetValue(args, ref i, arg), out var maxCategories))
                    {
                        throw new ArgumentException("Invalid value for --max-categories");
                    }
                    result.MaxCategories = maxCategories;
                    break;
            }
        }

        return result;
    }

    private static string GetValue(string[] args, ref int i, string arg)
    {
        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {arg}");
        }

        i++;
        return args[i];
    }
}

internal static class Scraper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] TopLevelCategorySeeds =
    {
        "/categoria-producto/protector/",
        "/categoria-producto/funda/",
        "/categoria-producto/accesorios/",
        "/categoria-producto/herramientas/",
        "/categoria-producto/patinete/"
    };

    public static async Task RunAsync(CliOptions options)
    {
        var store = new PersistentStore(options.OutputDir);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !options.Headful
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            Locale = "es-ES",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1440, Height = 2400 }
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(18000);

        if (!store.HasAnyQueueOrProcessed())
        {
            Console.WriteLine($"[+] Opening home page: {options.BaseUrl}");
            await page.GotoAsync(options.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForTimeoutAsync(2500);

            await AcceptCookiesAsync(page);

            // 1) Seed from mega menu
            var taxonomyRows = await ExtractMenuTaxonomyAsync(page, options.BaseUrl);
            foreach (var row in taxonomyRows)
            {
                store.AddCategory(row);

                if (!store.ProcessedCategoryUrls.Contains(row.CategoryUrl) &&
                    !store.QueuedCategoryUrls.Contains(row.CategoryUrl))
                {
                    store.QueuedCategoryUrls.Enqueue(row.CategoryUrl);
                }
            }

            // 2) Force top-level category pages first, because those are outside the mega-menu taxonomy path
            var topLevelRows = BuildTopLevelSeedRows(options.BaseUrl);
            foreach (var row in topLevelRows)
            {
                store.AddCategory(row);

                if (!store.ProcessedCategoryUrls.Contains(row.CategoryUrl) &&
                    !store.QueuedCategoryUrls.Contains(row.CategoryUrl))
                {
                    store.QueuedCategoryUrls.Enqueue(row.CategoryUrl);
                }
            }

            store.SaveState(options.BaseUrl);
        }

        var processedNow = 0;

        while (store.QueuedCategoryUrls.Count > 0)
        {
            var categoryUrl = store.QueuedCategoryUrls.Peek();

            if (store.ProcessedCategoryUrls.Contains(categoryUrl))
            {
                store.QueuedCategoryUrls.Dequeue();
                store.SaveState(options.BaseUrl);
                continue;
            }

            processedNow++;
            if (options.MaxCategories > 0 && processedNow > options.MaxCategories)
            {
                store.SaveState(options.BaseUrl);
                break;
            }

            var taxonomy = store.GetCategoryByUrl(categoryUrl) ?? new CategoryRow
            {
                CategoryUrl = categoryUrl,
                AnchorText = categoryUrl
            };

            Console.WriteLine($"[+] Visiting category: {categoryUrl}");

            try
            {
                await page.GotoAsync(categoryUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                await page.WaitForTimeoutAsync(2200);
                await AcceptCookiesAsync(page);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Failed category {categoryUrl}: {ex.Message}");

                store.QueuedCategoryUrls.Dequeue();
                store.QueuedCategoryUrls.Enqueue(categoryUrl);
                store.SaveState(options.BaseUrl);
                continue;
            }

            var loadMoreClicks = await ClickLoadMoreUntilDoneAsync(page);
            if (loadMoreClicks > 0)
            {
                Console.WriteLine($"    └─ load more clicked {loadMoreClicks} times");
            }

            // Discover category/subcategory links from current page as well
            var discoveredCategoryRows = await ExtractCategoryLinksFromCurrentPageAsync(page, categoryUrl, taxonomy);
            foreach (var row in discoveredCategoryRows)
            {
                store.AddCategory(row);

                if (!store.ProcessedCategoryUrls.Contains(row.CategoryUrl) &&
                    !store.QueuedCategoryUrls.Contains(row.CategoryUrl))
                {
                    store.QueuedCategoryUrls.Enqueue(row.CategoryUrl);
                }
            }

            var paginatedUrls = await HandleNumberedPaginationAsync(page, categoryUrl);
            foreach (var paginatedUrl in paginatedUrls)
            {
                var pagedCategory = taxonomy with { CategoryUrl = paginatedUrl };
                store.AddCategory(pagedCategory);

                if (!store.ProcessedCategoryUrls.Contains(paginatedUrl) &&
                    !store.QueuedCategoryUrls.Contains(paginatedUrl))
                {
                    store.QueuedCategoryUrls.Enqueue(paginatedUrl);
                }
            }

            store.SaveState(options.BaseUrl);

            var productLinks = await ExtractProductLinksFromCategoryAsync(page, categoryUrl);
            Console.WriteLine($"    └─ found {productLinks.Count} product links");

            var productSaveCounter = 0;

            foreach (var productUrl in productLinks)
            {
                if (store.HasProductUrl(productUrl))
                {
                    continue;
                }

                try
                {
                    var product = await ScrapeProductDetailAsync(page, productUrl, taxonomy);
                    if (store.AddProduct(product))
                    {
                        productSaveCounter++;
                    }

                    if (productSaveCounter >= 10)
                    {
                        store.SaveState(options.BaseUrl);
                        productSaveCounter = 0;
                    }

                    Console.WriteLine($"       ✓ {(string.IsNullOrWhiteSpace(product.ProductName) ? productUrl : product.ProductName)}");
                    await DelayAsync(400, 900);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"       ✗ failed product {productUrl}: {ex.Message}");
                }
            }

            store.QueuedCategoryUrls.Dequeue();
            store.ProcessedCategoryUrls.Add(categoryUrl);
            store.SaveState(options.BaseUrl);
            await DelayAsync(500, 1000);
        }

        var summary = new Summary
        {
            BaseUrl = options.BaseUrl,
            ProductsCount = store.SeenProducts.Count,
            CategoriesCount = store.SeenCategories.Count,
            ProcessedCategories = store.ProcessedCategoryUrls.Count,
            RemainingQueue = store.QueuedCategoryUrls.Count,
            OutputDir = Path.GetFullPath(options.OutputDir),
            GeneratedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        var summaryPath = Path.Combine(options.OutputDir, "summary.json");
        await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(summary, JsonOptions));
        Console.WriteLine(JsonSerializer.Serialize(summary, JsonOptions));

        await context.CloseAsync();
    }

    private static List<CategoryRow> BuildTopLevelSeedRows(string baseUrl)
    {
        var rows = new List<CategoryRow>();

        foreach (var seed in TopLevelCategorySeeds)
        {
            var fullUrl = NormalizeUrl(baseUrl, seed);
            if (string.IsNullOrWhiteSpace(fullUrl))
            {
                continue;
            }

            var slug = seed.Trim('/').Split('/').Last();
            var display = slug switch
            {
                "protector" => "Protector",
                "funda" => "Funda",
                "accesorios" => "Accesorios",
                "herramientas" => "Herramientas",
                "patinete" => "Patinete",
                _ => slug
            };

            rows.Add(new CategoryRow
            {
                CategoryUrl = fullUrl,
                Brand = string.Empty,
                Category = display,
                PartType = string.Empty,
                AnchorText = display,
                DiscoveredFrom = "top_level_seed"
            });
        }

        return rows;
    }

    private static async Task AcceptCookiesAsync(IPage page)
    {
        var selectors = new[]
        {
            "button:has-text('Aceptar')",
            "button:has-text('Accept')",
            "text=Aceptar",
            "#onetrust-accept-btn-handler"
        };

        foreach (var selector in selectors)
        {
            try
            {
                var locator = page.Locator(selector).First;
                if (await locator.CountAsync() > 0 && await locator.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 500 }))
                {
                    await locator.ClickAsync(new LocatorClickOptions { Timeout = 1500 });
                    await page.WaitForTimeoutAsync(600);
                    break;
                }
            }
            catch
            {
            }
        }
    }

    private static async Task<List<CategoryRow>> ExtractMenuTaxonomyAsync(IPage page, string baseUrl)
    {
        var rows = new List<CategoryRow>();
        var menu = page.Locator("#menu-recambio_es").First;

        if (await menu.CountAsync() == 0)
        {
            return rows;
        }

        var topItems = menu.Locator(":scope > li");
        var topCount = await topItems.CountAsync();

        for (var i = 0; i < topCount; i++)
        {
            var item = topItems.Nth(i);
            var brand = await SafeTextAsync(item.Locator(":scope > a .nav-link-text").First);

            var links = item.Locator("a[href]");
            var linkCount = await links.CountAsync();

            for (var j = 0; j < linkCount; j++)
            {
                var anchor = links.Nth(j);
                var href = await anchor.GetAttributeAsync("href");
                var url = NormalizeUrl(baseUrl, href);

                if (url is null || !LooksLikeCategory(url))
                {
                    continue;
                }

                var leafText = await SafeTextAsync(anchor);
                if (string.IsNullOrWhiteSpace(leafText))
                {
                    continue;
                }

                var category = string.Empty;
                try
                {
                    var panelTitle = await SafeTextAsync(
                        anchor.Locator("xpath=ancestor::*[contains(@class,'vc_tta-panel')][1]")
                              .Locator(".vc_tta-title-text")
                              .First);

                    if (!string.IsNullOrWhiteSpace(panelTitle))
                    {
                        category = panelTitle;
                    }
                }
                catch
                {
                }

                if (string.IsNullOrWhiteSpace(category))
                {
                    try
                    {
                        var groupLabel = await SafeTextAsync(
                            anchor.Locator("xpath=ancestor::*[contains(@class,'wpb_wrapper') or contains(@class,'vc_column-inner')][1]")
                                  .Locator(".nav-link-text")
                                  .First);

                        if (!string.IsNullOrWhiteSpace(groupLabel) &&
                            !string.Equals(groupLabel, leafText, StringComparison.OrdinalIgnoreCase))
                        {
                            category = groupLabel;
                        }
                    }
                    catch
                    {
                    }
                }

                rows.Add(new CategoryRow
                {
                    CategoryUrl = url,
                    Brand = brand,
                    Category = category,
                    PartType = leafText,
                    AnchorText = leafText,
                    DiscoveredFrom = "mega_menu"
                });
            }
        }

        return rows
            .GroupBy(x => x.CategoryUrl, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private static async Task<List<CategoryRow>> ExtractCategoryLinksFromCurrentPageAsync(IPage page, string currentUrl, CategoryRow parentTaxonomy)
    {
        var rows = new List<CategoryRow>();
        var links = await ExtractLinksAsync(page, currentUrl);

        foreach (var link in links)
        {
            if (!LooksLikeCategory(link.Url))
            {
                continue;
            }

            rows.Add(new CategoryRow
            {
                CategoryUrl = link.Url,
                Brand = parentTaxonomy.Brand,
                Category = !string.IsNullOrWhiteSpace(parentTaxonomy.Category) ? parentTaxonomy.Category : parentTaxonomy.AnchorText,
                PartType = link.Text,
                AnchorText = link.Text,
                DiscoveredFrom = currentUrl
            });
        }

        return rows
            .Where(x => !string.IsNullOrWhiteSpace(x.CategoryUrl))
            .GroupBy(x => x.CategoryUrl, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private static async Task<int> ClickLoadMoreUntilDoneAsync(IPage page)
    {
        var clicks = 0;
        var selectors = new[]
        {
            "text=Cargar más productos",
            "a:has-text('Cargar más productos')",
            "button:has-text('Cargar más productos')",
            ".load-more-button",
            ".ajax-load-more-wrap a"
        };

        while (true)
        {
            var clicked = false;
            var before = await page.Locator("a[href*='/producto/']").CountAsync();

            foreach (var selector in selectors)
            {
                try
                {
                    var button = page.Locator(selector).First;
                    if (await button.CountAsync() == 0 ||
                        !await button.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 800 }))
                    {
                        continue;
                    }

                    await button.ClickAsync(new LocatorClickOptions { Timeout = 3500 });
                    await page.WaitForTimeoutAsync(1800);

                    try
                    {
                        await page.WaitForFunctionAsync(
                            "(before) => document.querySelectorAll('a[href*=\\\"/producto/\\\"]').length > before",
                            before,
                            new PageWaitForFunctionOptions { Timeout = 5000 });
                    }
                    catch
                    {
                    }

                    clicks++;
                    clicked = true;
                    await DelayAsync(500, 1000);
                    break;
                }
                catch
                {
                }
            }

            var after = await page.Locator("a[href*='/producto/']").CountAsync();
            if (!clicked || after <= before)
            {
                break;
            }
        }

        return clicks;
    }

    private static async Task<List<string>> HandleNumberedPaginationAsync(IPage page, string currentUrl)
    {
        var links = await ExtractLinksAsync(page, currentUrl);
        return links
            .Select(x => x.Url)
            .Where(x => x.Contains("/page/", StringComparison.OrdinalIgnoreCase) && LooksLikeCategory(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<List<string>> ExtractProductLinksFromCategoryAsync(IPage page, string currentUrl)
    {
        var links = await ExtractLinksAsync(page, currentUrl);
        return links
            .Select(x => x.Url)
            .Where(LooksLikeProduct)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<ProductRow> ScrapeProductDetailAsync(IPage page, string productUrl, CategoryRow taxonomy)
    {
        await page.GotoAsync(productUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForTimeoutAsync(1800);

        var title = await TextOrContentAsync(page, new[] { "h1.product_title", "h1.entry-title", "h1" });
        var shortDescription = await TextOrContentAsync(page, new[] { ".woocommerce-product-details__short-description", ".short-description" });
        var longDescription = await TextOrContentAsync(page, new[] { "#tab-description", ".woocommerce-Tabs-panel--description", ".wd-accordion-content" });
        var specifications = await ParseSpecificationsAsync(page);
        var bodyText = await SafeTextAsync(page.Locator("body"));
        var (sku, barcode) = await GetSkuAndBarcodeAsync(page, bodyText);

        var imageUrl = string.Empty;
        try
        {
            var image = page.Locator(".woocommerce-product-gallery img, .wp-post-image, figure img").First;
            if (await image.CountAsync() > 0)
            {
                imageUrl = await image.GetAttributeAsync("src")
                           ?? await image.GetAttributeAsync("data-src")
                           ?? string.Empty;
            }
        }
        catch
        {
        }

        var badge = string.Empty;
        foreach (var keyword in new[] { "Agotado", "Oferta", "Sale", "Nuevo" })
        {
            if (bodyText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                badge = keyword;
                break;
            }
        }

        return new ProductRow
        {
            ProductUrl = productUrl,
            ProductName = title,
            Sku = sku,
            Barcode = barcode,
            Brand = taxonomy.Brand,
            Category = taxonomy.Category,
            PartType = taxonomy.PartType,
            ShortDescription = shortDescription,
            LongDescription = longDescription,
            Specifications = specifications,
            TrackingType = InferTrackingType(title, shortDescription, longDescription, specifications, bodyText),
            QualityType = InferQualityType(title, shortDescription, longDescription, specifications, bodyText),
            ImageUrl = imageUrl,
            Badge = badge,
            SourcePage = taxonomy.CategoryUrl,
            ScrapedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private static async Task<(string Sku, string Barcode)> GetSkuAndBarcodeAsync(IPage page, string bodyText)
    {
        var sku = await TextOrContentAsync(page, new[] { ".sku", "[class*='sku']", "span.sku_wrapper .sku" });
        sku = Regex.Replace(sku ?? string.Empty, @"(?i)\bsku\b[:\s-]*", string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(sku))
        {
            foreach (var pattern in new[]
                     {
                         @"(?i)\bsku\b[:\s#-]*([A-Z0-9._/-]{3,})",
                         @"(?i)\breferencia\b[:\s#-]*([A-Z0-9._/-]{3,})"
                     })
            {
                var match = Regex.Match(bodyText, pattern);
                if (match.Success)
                {
                    sku = match.Groups[1].Value.Trim();
                    break;
                }
            }
        }

        var barcode = string.Empty;
        foreach (var pattern in new[]
                 {
                     @"(?i)\bbarcode\b[:\s#-]*([0-9A-Z-]{6,})",
                     @"(?i)\bean\b[:\s#-]*([0-9A-Z-]{6,})",
                     @"(?i)\bupc\b[:\s#-]*([0-9A-Z-]{6,})",
                     @"(?i)\bgtin\b[:\s#-]*([0-9A-Z-]{6,})"
                 })
        {
            var match = Regex.Match(bodyText, pattern);
            if (match.Success)
            {
                barcode = match.Groups[1].Value.Trim();
                break;
            }
        }

        return (sku, barcode);
    }

    private static async Task<string> ParseSpecificationsAsync(IPage page)
    {
        var specs = new List<string>();

        foreach (var selector in new[]
                 {
                     "table.shop_attributes",
                     "table.woocommerce-product-attributes",
                     ".woocommerce-product-attributes",
                     ".wd-accordion-content table"
                 })
        {
            try
            {
                var table = page.Locator(selector).First;
                if (await table.CountAsync() > 0)
                {
                    var text = await SafeTextAsync(table);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        specs.Add(text);
                    }
                }
            }
            catch
            {
            }
        }

        foreach (var selector in new[]
                 {
                     ".woocommerce-Tabs-panel--additional_information",
                     "#tab-additional_information",
                     ".product-tabs-content"
                 })
        {
            try
            {
                var node = page.Locator(selector).First;
                if (await node.CountAsync() > 0)
                {
                    var text = await SafeTextAsync(node);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        specs.Add(text);
                    }
                }
            }
            catch
            {
            }
        }

        return string.Join(" | ", specs.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal));
    }

    private static async Task<string> TextOrContentAsync(IPage page, IEnumerable<string> selectors)
    {
        foreach (var selector in selectors)
        {
            try
            {
                var locator = page.Locator(selector).First;
                if (await locator.CountAsync() > 0)
                {
                    var text = await SafeTextAsync(locator);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }
            catch
            {
            }
        }

        return string.Empty;
    }

    private static async Task<List<LinkItem>> ExtractLinksAsync(IPage page, string baseUrl)
    {
        var results = new List<LinkItem>();
        var anchors = page.Locator("a[href]");
        var count = await anchors.CountAsync();

        for (var i = 0; i < count; i++)
        {
            var anchor = anchors.Nth(i);
            string? href = null;

            try
            {
                href = await anchor.GetAttributeAsync("href", new LocatorGetAttributeOptions { Timeout = 1000 });
            }
            catch
            {
            }

            var url = NormalizeUrl(baseUrl, href);
            if (url is null || !IsSameDomain(baseUrl, url))
            {
                continue;
            }

            var text = await SafeTextAsync(anchor);
            results.Add(new LinkItem(url, text));
        }

        return results;
    }

    private static string NormalizeText(string? input)
        => Regex.Replace(input ?? string.Empty, @"\s+", " ").Trim();

    private static async Task<string> SafeTextAsync(ILocator locator)
    {
        try
        {
            var text = await locator.InnerTextAsync(new LocatorInnerTextOptions { Timeout = 1200 });
            return NormalizeText(text);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool LooksLikeCategory(string url) => url.Contains("/categoria-producto/", StringComparison.OrdinalIgnoreCase);
    private static bool LooksLikeProduct(string url) => url.Contains("/producto/", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeUrl(string baseUrl, string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        href = href.Trim();

        if (href.StartsWith("#", StringComparison.Ordinal) ||
            href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!Uri.TryCreate(new Uri(baseUrl), href, out var uri))
        {
            return null;
        }

        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            return null;
        }

        var path = uri.AbsolutePath.ToLowerInvariant();
        var blacklist = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg", ".pdf", ".zip", ".rar" };
        if (blacklist.Any(ext => path.EndsWith(ext, StringComparison.Ordinal)))
        {
            return null;
        }

        var normalized = uri.ToString().TrimEnd('/');
        return normalized + "/";
    }

    private static bool IsSameDomain(string root, string url)
    {
        var rootHost = new Uri(root).Host;
        var urlHost = new Uri(url).Host;
        return string.Equals(rootHost, urlHost, StringComparison.OrdinalIgnoreCase);
    }

    private static string InferQualityType(params string[] texts)
    {
        var joined = string.Join(" ", texts.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();

        if (joined.Contains("refurb") || joined.Contains("reacond"))
            return "Refurbished";
        if (joined.Contains("original"))
            return "Original";
        if (Regex.IsMatch(joined, @"\boem\b"))
            return "OEM";
        if (joined.Contains("high copy") || joined.Contains("highcopy"))
            return "HighCopy";

        return string.Empty;
    }

    private static string InferTrackingType(params string[] texts)
    {
        var joined = string.Join(" ", texts.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();

        if (joined.Contains("serial") || joined.Contains("imei") || joined.Contains("serialized"))
            return "Serialized";

        return "Quantity Based";
    }

    private static Task DelayAsync(int minMs, int maxMs)
        => Task.Delay(Random.Shared.Next(minMs, maxMs + 1));
}

internal sealed class PersistentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string OutputDir { get; }
    public string ProductsJsonlPath { get; }
    public string ProductsCsvPath { get; }
    public string CategoriesJsonlPath { get; }
    public string CategoriesCsvPath { get; }
    public string SeenProductsPath { get; }
    public string SeenCategoriesPath { get; }
    public string StatePath { get; }

    public HashSet<string> SeenProducts { get; }
    public HashSet<string> SeenCategories { get; }
    public HashSet<string> ProcessedCategoryUrls { get; }
    public Queue<string> QueuedCategoryUrls { get; }

    private readonly Dictionary<string, CategoryRow> _categoriesByUrl;

    public PersistentStore(string outputDir)
    {
        OutputDir = outputDir;
        Directory.CreateDirectory(OutputDir);

        ProductsJsonlPath = Path.Combine(OutputDir, "products.jsonl");
        ProductsCsvPath = Path.Combine(OutputDir, "products.csv");
        CategoriesJsonlPath = Path.Combine(OutputDir, "categories.jsonl");
        CategoriesCsvPath = Path.Combine(OutputDir, "categories.csv");
        SeenProductsPath = Path.Combine(OutputDir, "seen_products.json");
        SeenCategoriesPath = Path.Combine(OutputDir, "seen_categories.json");
        StatePath = Path.Combine(OutputDir, "state.json");

        SeenProducts = LoadStringSet(SeenProductsPath);
        SeenCategories = LoadStringSet(SeenCategoriesPath);

        var state = LoadState(StatePath);
        ProcessedCategoryUrls = state.ProcessedCategoryUrls;
        QueuedCategoryUrls = new Queue<string>(state.QueuedCategoryUrls);

        _categoriesByUrl = LoadCategories(CategoriesJsonlPath);

        EnsureCsvHeaders();
    }

    public bool HasAnyQueueOrProcessed()
        => QueuedCategoryUrls.Count > 0 || ProcessedCategoryUrls.Count > 0;

    public CategoryRow? GetCategoryByUrl(string url)
        => _categoriesByUrl.TryGetValue(url, out var row) ? row : null;

    public bool AddCategory(CategoryRow row)
    {
        if (string.IsNullOrWhiteSpace(row.CategoryUrl))
        {
            return false;
        }

        var key = row.CategoryUrl.Trim();
        if (SeenCategories.Contains(key))
        {
            return false;
        }

        SeenCategories.Add(key);
        _categoriesByUrl[key] = row;

        File.AppendAllText(CategoriesJsonlPath, JsonSerializer.Serialize(row) + Environment.NewLine);
        AppendCsv(CategoriesCsvPath, row);

        return true;
    }

    public bool AddProduct(ProductRow row)
    {
        var key = ProductKey(row);
        if (string.IsNullOrWhiteSpace(key) || SeenProducts.Contains(key))
        {
            return false;
        }

        SeenProducts.Add(key);

        File.AppendAllText(ProductsJsonlPath, JsonSerializer.Serialize(row) + Environment.NewLine);
        AppendCsv(ProductsCsvPath, row);

        return true;
    }

    public bool HasProductUrl(string productUrl)
    {
        var normalizedUrl = productUrl.Trim().TrimEnd('/').ToLowerInvariant();
        return SeenProducts.Contains($"url::{normalizedUrl}/") ||
               SeenProducts.Contains($"url::{normalizedUrl}");
    }

    public void SaveState(string baseUrl)
    {
        var state = new StateFile
        {
            BaseUrl = baseUrl,
            UpdatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ProductsCount = SeenProducts.Count,
            CategoriesCount = SeenCategories.Count,
            QueuedCategoryUrls = QueuedCategoryUrls.ToList(),
            ProcessedCategoryUrls = new HashSet<string>(
                ProcessedCategoryUrls.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase)
        };

        File.WriteAllText(StatePath, JsonSerializer.Serialize(state, JsonOptions));
        File.WriteAllText(SeenProductsPath, JsonSerializer.Serialize(SeenProducts.OrderBy(x => x).ToList(), JsonOptions));
        File.WriteAllText(SeenCategoriesPath, JsonSerializer.Serialize(SeenCategories.OrderBy(x => x).ToList(), JsonOptions));
    }

    private void EnsureCsvHeaders()
    {
        if (!File.Exists(ProductsCsvPath))
        {
            using var writer = new StreamWriter(ProductsCsvPath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteHeader<ProductRow>();
            csv.NextRecord();
        }

        if (!File.Exists(CategoriesCsvPath))
        {
            using var writer = new StreamWriter(CategoriesCsvPath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteHeader<CategoryRow>();
            csv.NextRecord();
        }
    }

    private static void AppendCsv<T>(string path, T row)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        };

        using var stream = new StreamWriter(path, append: true);
        using var csv = new CsvWriter(stream, config);
        csv.WriteRecord(row);
        csv.NextRecord();
    }

    private static string ProductKey(ProductRow row)
    {
        var sku = NormalizeKey(row.Sku);
        if (!string.IsNullOrWhiteSpace(sku))
        {
            return $"sku::{sku}";
        }

        var url = NormalizeKey(row.ProductUrl);
        if (!string.IsNullOrWhiteSpace(url))
        {
            return $"url::{url}";
        }

        return string.Empty;
    }

    private static string NormalizeKey(string? input)
        => (input ?? string.Empty).Trim().TrimEnd('/').ToLowerInvariant();

    private static HashSet<string> LoadStringSet(string path)
    {
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path)) ?? new List<string>();
            return new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static StateFile LoadState(string path)
    {
        if (!File.Exists(path))
        {
            return new StateFile();
        }

        try
        {
            return JsonSerializer.Deserialize<StateFile>(File.ReadAllText(path)) ?? new StateFile();
        }
        catch
        {
            return new StateFile();
        }
    }

    private static Dictionary<string, CategoryRow> LoadCategories(string path)
    {
        var result = new Dictionary<string, CategoryRow>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(path))
        {
            return result;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var row = JsonSerializer.Deserialize<CategoryRow>(line);
                if (row is not null && !string.IsNullOrWhiteSpace(row.CategoryUrl))
                {
                    result[row.CategoryUrl] = row;
                }
            }
            catch
            {
            }
        }

        return result;
    }
}

internal sealed record LinkItem(string Url, string Text);

internal sealed record CategoryRow
{
    public string CategoryUrl { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string PartType { get; init; } = string.Empty;
    public string AnchorText { get; init; } = string.Empty;
    public string DiscoveredFrom { get; init; } = string.Empty;
}

internal sealed record ProductRow
{
    public string ProductUrl { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string PartType { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string LongDescription { get; init; } = string.Empty;
    public string Specifications { get; init; } = string.Empty;
    public string TrackingType { get; init; } = string.Empty;
    public string QualityType { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string Badge { get; init; } = string.Empty;
    public string SourcePage { get; init; } = string.Empty;
    public string ScrapedAt { get; init; } = string.Empty;
}

internal sealed record StateFile
{
    public string BaseUrl { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public int ProductsCount { get; init; }
    public int CategoriesCount { get; init; }
    public List<string> QueuedCategoryUrls { get; init; } = new List<string>();
    public HashSet<string> ProcessedCategoryUrls { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

internal sealed record Summary
{
    public string BaseUrl { get; init; } = string.Empty;
    public int ProductsCount { get; init; }
    public int CategoriesCount { get; init; }
    public int ProcessedCategories { get; init; }
    public int RemainingQueue { get; init; }
    public string OutputDir { get; init; } = string.Empty;
    public string GeneratedAt { get; init; } = string.Empty;
}
