using System;
using System.Net;
using System.Threading.RateLimiting;
using CaseShop.Web.Services.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CaseShop.Web.Common;

public static class SecurityExtensions
{
    public const string AdminLoginPolicy = "AdminLoginPolicy";
    public const string CheckoutPolicy = "CheckoutPolicy";

    /// <summary>
    /// Registers essential security services, rate limiters, and reverse proxy forwarders.
    /// </summary>
    public static IServiceCollection AddCaseShopSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Singleton in-memory rate limiters for business logic (Brute force & Order flood)
        services.AddSingleton<ILoginRateLimiter, LoginRateLimiter>();
        services.AddSingleton<IOrderRateLimiter, OrderRateLimiter>();

        // 2. Configure ForwardedHeaders for Nginx / AWS ALB reverse proxies
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // 3. ASP.NET Core Built-in Rate Limiting Middleware (Anti-DoS)
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await context.HttpContext.Response.WriteAsync(
                    "Yêu cầu quá nhiều lần trong khoảng thời gian ngắn. Vui lòng thử lại sau ít phút.",
                    cancellationToken: cancellationToken);
            };

            // Global Sliding Window Rate Limiter (Protects the whole application from DoS floods)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var clientIp = ResolveClientIp(httpContext);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 150,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0
                    });
            });

            // Strict Policy for Admin Login Endpoints
            options.AddPolicy(AdminLoginPolicy, httpContext =>
            {
                var clientIp = ResolveClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            // Policy for Checkout Endpoints
            options.AddPolicy(CheckoutPolicy, httpContext =>
            {
                var clientIp = ResolveClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        // 4. Configure HSTS for Production
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });

        return services;
    }

    /// <summary>
    /// Middleware to enforce essential HTTP Security Headers on all HTTP responses.
    /// </summary>
    public static IApplicationBuilder UseCaseShopSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            // Prevent Clickjacking (disallow embedding in external iframes)
            headers["X-Frame-Options"] = "SAMEORIGIN";

            // Prevent MIME-sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Enable legacy XSS filter in browsers
            headers["X-XSS-Protection"] = "1; mode=block";

            // Control referrer information leakage
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Disable unwanted browser device features
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            // Content Security Policy tailored for Blazor Server + Tailwind CSS + Google Fonts + Cloudinary + SignalR WebSockets
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com data:; " +
                "img-src 'self' data: blob: https://res.cloudinary.com https://images.unsplash.com; " +
                "connect-src 'self' ws: wss:; " +
                "frame-ancestors 'self';";

            await next();
        });
    }

    private static string ResolveClientIp(HttpContext httpContext)
    {
        // 1. Check X-Forwarded-For header first (when behind Nginx or Cloudflare)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (ips.Length > 0 && IPAddress.TryParse(ips[0], out var parsedIp))
            {
                return parsedIp.ToString();
            }
        }

        // 2. Fall back to RemoteIpAddress
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";
    }
}
