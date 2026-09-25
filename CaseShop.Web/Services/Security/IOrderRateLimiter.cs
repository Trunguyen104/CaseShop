using System;

namespace CaseShop.Web.Services.Security;

/// <summary>
/// Service to prevent automated order spamming and Denial of Service on the checkout process.
/// </summary>
public interface IOrderRateLimiter
{
    /// <summary>
    /// Checks if a client IP or phone number has exceeded order creation limits.
    /// </summary>
    bool IsRateLimited(string? ipAddress, string? phone, out TimeSpan retryAfter);

    /// <summary>
    /// Records a newly placed order for rate tracking.
    /// </summary>
    void RecordOrder(string? ipAddress, string? phone);
}
