using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CaseShop.Web.Services.Security;

/// <summary>
/// In-memory sliding-window rate limiter for order submissions.
/// Allows a maximum of 5 orders per 5-minute window per IP address or phone number.
/// </summary>
public class OrderRateLimiter : IOrderRateLimiter
{
    private const int MaxOrdersPerWindow = 5;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, List<DateTime>> _orderTimestamps = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<OrderRateLimiter> _logger;
    private DateTime _lastCleanupUtc = DateTime.UtcNow;

    public OrderRateLimiter(ILogger<OrderRateLimiter> logger)
    {
        _logger = logger;
    }

    public bool IsRateLimited(string? ipAddress, string? phone, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        CleanupIfNecessary();
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(ipAddress) && CheckRateLimit($"ip:{ipAddress.Trim()}", now, out retryAfter))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(phone) && CheckRateLimit($"phone:{phone.Trim()}", now, out retryAfter))
        {
            return true;
        }

        return false;
    }

    public void RecordOrder(string? ipAddress, string? phone)
    {
        CleanupIfNecessary();
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            RecordInternal($"ip:{ipAddress.Trim()}", now);
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            RecordInternal($"phone:{phone.Trim()}", now);
        }
    }

    private bool CheckRateLimit(string key, DateTime now, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;

        if (_orderTimestamps.TryGetValue(key, out var timestamps))
        {
            lock (timestamps)
            {
                // Remove timestamps older than window
                timestamps.RemoveAll(t => now - t > WindowDuration);

                if (timestamps.Count >= MaxOrdersPerWindow)
                {
                    var oldestInWindow = timestamps[0];
                    retryAfter = WindowDuration - (now - oldestInWindow);
                    if (retryAfter < TimeSpan.FromSeconds(1))
                    {
                        retryAfter = TimeSpan.FromSeconds(1);
                    }

                    _logger.LogWarning("Order rate limit hit for '{Key}'. Orders in window: {Count}. Retry after: {Seconds}s.",
                        key, timestamps.Count, (int)retryAfter.TotalSeconds);
                    return true;
                }
            }
        }

        return false;
    }

    private void RecordInternal(string key, DateTime now)
    {
        var list = _orderTimestamps.GetOrAdd(key, _ => new List<DateTime>());
        lock (list)
        {
            list.RemoveAll(t => now - t > WindowDuration);
            list.Add(now);
        }
    }

    private void CleanupIfNecessary()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanupUtc < TimeSpan.FromMinutes(5))
        {
            return;
        }

        _lastCleanupUtc = now;

        foreach (var (key, timestamps) in _orderTimestamps.ToArray())
        {
            lock (timestamps)
            {
                timestamps.RemoveAll(t => now - t > WindowDuration);
                if (timestamps.Count == 0)
                {
                    _orderTimestamps.TryRemove(key, out _);
                }
            }
        }
    }
}
