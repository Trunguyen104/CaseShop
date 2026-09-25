using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaseShop.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CaseShop.Web.Data;

public static class DbSeeder
{
    public static async Task SeedDemoDataAsync(AppDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting development demo data seed...");

        // 1. Phone Brands
        var brands = new List<PhoneBrand>
        {
            new PhoneBrand { Id = Guid.NewGuid(), Name = "Apple", Slug = "apple", IsActive = true, CreatedAt = DateTime.UtcNow },
            new PhoneBrand { Id = Guid.NewGuid(), Name = "Samsung", Slug = "samsung", IsActive = true, CreatedAt = DateTime.UtcNow },
            new PhoneBrand { Id = Guid.NewGuid(), Name = "Google", Slug = "google", IsActive = true, CreatedAt = DateTime.UtcNow },
            new PhoneBrand { Id = Guid.NewGuid(), Name = "Xiaomi", Slug = "xiaomi", IsActive = true, CreatedAt = DateTime.UtcNow },
            new PhoneBrand { Id = Guid.NewGuid(), Name = "Demo Archived Brand", Slug = "demo-archived-brand", IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        foreach (var b in brands)
        {
            if (!await context.PhoneBrands.AnyAsync(x => x.Slug == b.Slug))
            {
                context.PhoneBrands.Add(b);
            }
        }
        await context.SaveChangesAsync();

        var apple = await context.PhoneBrands.FirstAsync(x => x.Slug == "apple");
        var samsung = await context.PhoneBrands.FirstAsync(x => x.Slug == "samsung");
        var google = await context.PhoneBrands.FirstAsync(x => x.Slug == "google");
        var xiaomi = await context.PhoneBrands.FirstAsync(x => x.Slug == "xiaomi");

        // Dọn dẹp bản ghi cũ thử nghiệm nếu có
        var legacyInvalid = await context.PhoneModels.Where(x => x.Slug == "iphone").ToListAsync();
        if (legacyInvalid.Count > 0)
        {
            context.PhoneModels.RemoveRange(legacyInvalid);
            await context.SaveChangesAsync();
        }

        // 2. Phone Models
        var models = new List<PhoneModel>
        {
            CreatePhoneModel(apple.Id, "iPhone 15", "iphone-15", true),
            CreatePhoneModel(apple.Id, "iPhone 15 Pro", "iphone-15-pro", true),
            CreatePhoneModel(apple.Id, "iPhone 16", "iphone-16", true),
            CreatePhoneModel(apple.Id, "iPhone 16 Pro", "iphone-16-pro", true),
            
            CreatePhoneModel(samsung.Id, "Samsung Galaxy S24", "galaxy-s24", true),
            CreatePhoneModel(samsung.Id, "Samsung Galaxy S24 Ultra", "galaxy-s24-ultra", true),
            CreatePhoneModel(samsung.Id, "Samsung Galaxy S25", "galaxy-s25", true),
            CreatePhoneModel(samsung.Id, "Samsung Galaxy S25 Ultra", "galaxy-s25-ultra", true),

            CreatePhoneModel(google.Id, "Pixel 8", "pixel-8", true),
            CreatePhoneModel(google.Id, "Pixel 8 Pro", "pixel-8-pro", true),
            CreatePhoneModel(google.Id, "Pixel 9", "pixel-9", true),
            CreatePhoneModel(google.Id, "Pixel 9 Pro", "pixel-9-pro", true),

            CreatePhoneModel(xiaomi.Id, "Xiaomi 14", "xiaomi-14", true),
            CreatePhoneModel(xiaomi.Id, "Xiaomi 14 Ultra", "xiaomi-14-ultra", true),

            CreatePhoneModel(apple.Id, "Demo Archived Phone", "demo-archived-phone", false)
        };

        foreach (var m in models)
        {
            var existing = await context.PhoneModels.FirstOrDefaultAsync(x => x.Slug == m.Slug);
            if (existing == null)
            {
                context.PhoneModels.Add(m);
            }
            else
            {
                // Cập nhật thông số mẫu cụm camera và kích thước cho dữ liệu đã có
                existing.CanvasWidth = m.CanvasWidth;
                existing.CanvasHeight = m.CanvasHeight;
                existing.CornerRadius = m.CornerRadius;
                existing.SafeAreaX = m.SafeAreaX;
                existing.SafeAreaY = m.SafeAreaY;
                existing.SafeAreaWidth = m.SafeAreaWidth;
                existing.SafeAreaHeight = m.SafeAreaHeight;
                existing.PrintAreaX = m.PrintAreaX;
                existing.PrintAreaY = m.PrintAreaY;
                existing.PrintAreaWidth = m.PrintAreaWidth;
                existing.PrintAreaHeight = m.PrintAreaHeight;
                existing.BleedTop = m.BleedTop;
                existing.BleedRight = m.BleedRight;
                existing.BleedBottom = m.BleedBottom;
                existing.BleedLeft = m.BleedLeft;
                existing.CameraCutoutX = m.CameraCutoutX;
                existing.CameraCutoutY = m.CameraCutoutY;
                existing.CameraCutoutWidth = m.CameraCutoutWidth;
                existing.CameraCutoutHeight = m.CameraCutoutHeight;
                existing.CameraCutoutRadius = m.CameraCutoutRadius;
            }
        }
        await context.SaveChangesAsync();

        // 3. Products
        var products = new List<Product>
        {
            new Product { Id = Guid.NewGuid(), Name = "Classic Clear Case", Description = "Minimalist clear case", Price = 150000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Matte Black Case", Description = "Sleek matte black finish", Price = 200000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Minimal White Case", Description = "Clean minimal white case", Price = 180000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Floral Case", Description = "Beautiful floral patterns", Price = 250000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Ocean Blue Case", Description = "Deep ocean blue color", Price = 220000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Sunset Case", Description = "Vibrant sunset gradients", Price = 230000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Abstract Line Case", Description = "Modern abstract line art", Price = 240000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Custom Photo Case", Description = "Print your own photos", Price = 300000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = Guid.NewGuid(), Name = "Demo Archived Product", Description = "Inactive product", Price = 99000, IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        foreach (var p in products)
        {
            if (!await context.Products.AnyAsync(x => x.Name == p.Name))
            {
                context.Products.Add(p);
            }
        }
        await context.SaveChangesAsync();

        // 4. Stickers
        var stickers = new List<Sticker>
        {
            new Sticker { Id = Guid.NewGuid(), Name = "Heart", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%23ef4444' d='M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Star", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%23facc15' d='M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Smile", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><circle cx='12' cy='12' r='10' fill='%23fbbf24'/><circle cx='8.5' cy='9.5' r='1.5' fill='%231f2937'/><circle cx='15.5' cy='9.5' r='1.5' fill='%231f2937'/><path fill='none' stroke='%231f2937' stroke-width='2' stroke-linecap='round' d='M8 14.5c1.2 1.8 2.8 2.5 4 2.5s2.8-.7 4-2.5'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Flower", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><circle cx='12' cy='6' r='4' fill='%23f43f5e'/><circle cx='12' cy='18' r='4' fill='%23f43f5e'/><circle cx='6' cy='12' r='4' fill='%23f43f5e'/><circle cx='18' cy='12' r='4' fill='%23f43f5e'/><circle cx='12' cy='12' r='4' fill='%23fbbf24'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Lightning", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%23f59e0b' d='M7 2v11h3v9l7-12h-4l4-8z'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Crown", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%23eab308' d='M5 16L3 5l5.5 5L12 4l3.5 6L21 5l-2 11H5zm14 3c0 .55-.45 1-1 1H6c-.55 0-1-.45-1-1v-1h14v1z'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Camera", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%233b82f6' d='M9 2L7.17 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2h-3.17L15 2H9zm3 15c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5z'/><circle cx='12' cy='12' r='3.2' fill='%23ffffff'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Music", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%238b5cf6' d='M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Coffee", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%2378350f' d='M20 3H4v10c0 2.21 1.79 4 4 4h6c2.21 0 4-1.79 4-4v-3h2c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 5h-2V5h2v3zM2 21h18v-2H2v2z'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Travel", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%2306b6d4' d='M21 16v-2l-8-5V3.5c0-.83-.67-1.5-1.5-1.5S10 2.67 10 3.5V9l-8 5v2l8-2.5V19l-2 1.5V22l3.5-1 3.5 1v-1.5L13 19v-5.5l8 2.5z'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Cat", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%23f97316' d='M12 4C7.58 4 4 7.58 4 12c0 2.21.9 4.21 2.34 5.66L4 21l3.34-1.34C8.79 20.32 10.33 20.7 12 20.7c4.42 0 8-3.58 8-8s-3.58-8-8-8zm-3 8c-.83 0-1.5-.67-1.5-1.5S8.17 9 9 9s1.5.67 1.5 1.5S9.83 12 9 12zm6 0c-.83 0-1.5-.67-1.5-1.5S14.17 9 15 9s1.5.67 1.5 1.5S15.83 12 15 12z'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Dog", ImageUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='120' height='120'><path fill='%23a16207' d='M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-3.5 8c.83 0 1.5.67 1.5 1.5S9.33 13 8.5 13 7 12.33 7 11.5 7.67 10 8.5 10zm7 0c.83 0 1.5.67 1.5 1.5s-.67 1.5-1.5 1.5-1.5-.67-1.5-1.5.67-1.5 1.5-1.5zM12 17.5c-1.38 0-2.5-.67-2.5-1.5h5c0 .83-1.12 1.5-2.5 1.5z'/></svg>", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sticker { Id = Guid.NewGuid(), Name = "Demo Archived Sticker", IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        foreach (var s in stickers)
        {
            var existing = await context.Stickers.FirstOrDefaultAsync(x => x.Name == s.Name);
            if (existing == null)
            {
                context.Stickers.Add(s);
            }
            else if (string.IsNullOrEmpty(existing.ImageUrl) && !string.IsNullOrEmpty(s.ImageUrl))
            {
                existing.ImageUrl = s.ImageUrl;
            }
        }
        await context.SaveChangesAsync();

        logger.LogInformation("Demo data seed completed successfully.");
    }

    private static PhoneModel CreatePhoneModel(Guid brandId, string name, string slug, bool isActive)
    {
        decimal canvasW = 350, canvasH = 700, cornerR = 40;
        decimal cutoutX = 20, cutoutY = 20, cutoutW = 100, cutoutH = 100, cutoutR = 24;

        switch (slug)
        {
            case "iphone-15":
                canvasW = 350; canvasH = 700; cornerR = 44;
                cutoutX = 18; cutoutY = 18; cutoutW = 96; cutoutH = 96; cutoutR = 26;
                break;
            case "iphone-15-pro":
                canvasW = 350; canvasH = 700; cornerR = 44;
                cutoutX = 18; cutoutY = 18; cutoutW = 110; cutoutH = 114; cutoutR = 28;
                break;
            case "iphone-16":
                canvasW = 350; canvasH = 700; cornerR = 46;
                cutoutX = 18; cutoutY = 18; cutoutW = 72; cutoutH = 130; cutoutR = 36;
                break;
            case "iphone-16-pro":
                canvasW = 350; canvasH = 700; cornerR = 46;
                cutoutX = 18; cutoutY = 18; cutoutW = 112; cutoutH = 118; cutoutR = 28;
                break;
            case "galaxy-s24":
            case "galaxy-s25":
                canvasW = 350; canvasH = 700; cornerR = 38;
                cutoutX = 20; cutoutY = 22; cutoutW = 60; cutoutH = 155; cutoutR = 20;
                break;
            case "galaxy-s24-ultra":
            case "galaxy-s25-ultra":
                canvasW = 365; canvasH = 720; cornerR = 18;
                cutoutX = 20; cutoutY = 24; cutoutW = 76; cutoutH = 175; cutoutR = 14;
                break;
            case "pixel-8":
                canvasW = 350; canvasH = 700; cornerR = 48;
                cutoutX = 10; cutoutY = 72; cutoutW = 330; cutoutH = 68; cutoutR = 16;
                break;
            case "pixel-8-pro":
                canvasW = 360; canvasH = 720; cornerR = 48;
                cutoutX = 10; cutoutY = 72; cutoutW = 340; cutoutH = 72; cutoutR = 16;
                break;
            case "pixel-9":
                canvasW = 350; canvasH = 700; cornerR = 48;
                cutoutX = 20; cutoutY = 60; cutoutW = 310; cutoutH = 74; cutoutR = 37;
                break;
            case "pixel-9-pro":
                canvasW = 360; canvasH = 720; cornerR = 48;
                cutoutX = 20; cutoutY = 60; cutoutW = 320; cutoutH = 76; cutoutR = 38;
                break;
            case "xiaomi-14":
                canvasW = 350; canvasH = 700; cornerR = 42;
                cutoutX = 18; cutoutY = 18; cutoutW = 130; cutoutH = 130; cutoutR = 24;
                break;
            case "xiaomi-14-ultra":
                canvasW = 360; canvasH = 720; cornerR = 40;
                cutoutX = 75; cutoutY = 45; cutoutW = 210; cutoutH = 210; cutoutR = 105;
                break;
            default:
                if (slug.Contains("ultra"))
                {
                    canvasW = 365; canvasH = 720; cornerR = 18;
                    cutoutX = 20; cutoutY = 25; cutoutW = 75; cutoutH = 165; cutoutR = 14;
                }
                else if (slug.Contains("pixel"))
                {
                    cutoutX = 15; cutoutY = 70; cutoutW = 320; cutoutH = 65; cutoutR = 16;
                }
                break;
        }

        return new PhoneModel
        {
            Id = Guid.NewGuid(),
            PhoneBrandId = brandId,
            Name = name,
            Slug = slug,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CanvasWidth = canvasW,
            CanvasHeight = canvasH,
            CornerRadius = cornerR,
            SafeAreaX = 14,
            SafeAreaY = 14,
            SafeAreaWidth = canvasW - 28,
            SafeAreaHeight = canvasH - 28,
            PrintAreaX = 0,
            PrintAreaY = 0,
            PrintAreaWidth = canvasW,
            PrintAreaHeight = canvasH,
            BleedTop = 5,
            BleedRight = 5,
            BleedBottom = 5,
            BleedLeft = 5,
            CameraCutoutX = cutoutX,
            CameraCutoutY = cutoutY,
            CameraCutoutWidth = cutoutW,
            CameraCutoutHeight = cutoutH,
            CameraCutoutRadius = cutoutR,
            MaskImageUrl = null,
            OverlayImageUrl = null
        };
    }
}
