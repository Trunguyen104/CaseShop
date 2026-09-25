using System;
using CaseShop.Web.Common;
using CaseShop.Web.Components;
using CaseShop.Web.Data;
using CaseShop.Web.Repositories;
using CaseShop.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Load environment variables from .env if present
EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// Hide Kestrel Server header for information security
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32MB
    });

builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32MB
    });

builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32MB
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "CaseShop.Auth";
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.AccessDeniedPath = "/admin/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Core Application Security (Rate limiting, Reverse proxy headers, Security headers)
builder.Services.AddCaseShopSecurity(builder.Configuration);

// Repositories
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IStickerRepository, StickerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStickerService, StickerService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IPhoneCatalogService, PhoneCatalogService>();

var app = builder.Build();

// Apply database migrations and seed default data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    // 1. Automatically apply any pending EF Core migrations to ensure tables exist
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Applying EF Core database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations at startup.");
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }

    // 2. Seed default admin account if not present
    try
    {
        var authService = services.GetRequiredService<IAuthService>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var adminUsername = configuration["AdminCredentials:Username"]
            ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME")
            ?? "admin";

        var adminPassword = configuration["AdminCredentials:Password"]
            ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            if (app.Environment.IsDevelopment())
            {
                adminPassword = "Admin@123";
                logger.LogWarning("⚠️ No ADMIN_PASSWORD configured. Using fallback development password. Please configure ADMIN_PASSWORD in your .env file!");
            }
            else
            {
                logger.LogError("🚨 CRITICAL SECURITY ERROR: ADMIN_PASSWORD environment variable is not configured. Admin account cannot be seeded with an empty password in Production!");
                throw new InvalidOperationException("ADMIN_PASSWORD environment variable must be set in Production.");
            }
        }

        await authService.EnsureDefaultAdminCreatedAsync(adminUsername, adminPassword);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Could not verify/seed default admin account at startup.");
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }

    if (app.Environment.IsDevelopment())
    {
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            await DbSeeder.SeedDemoDataAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding demo data.");
        }
    }
}

// 1. Process Reverse Proxy Headers (Nginx / Cloudflare / AWS ELB)
app.UseForwardedHeaders();

// 2. Enforce HTTP Security Headers (X-Frame-Options, X-Content-Type-Options: nosniff, CSP, etc.)
app.UseCaseShopSecurityHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// 3. Anti-DoS and Brute-force HTTP Rate Limiting
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
