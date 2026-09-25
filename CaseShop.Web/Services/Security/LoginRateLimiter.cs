using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CaseShop.Web.Services.Security;

/// <summary>
/// In-memory, thread-safe rate limiter and brute-force protector for admin authentication.
/// Limits failed attempts to 5 per 10 minutes, with a 15-minute lockout period.
/// </summary>
public class LoginRateLimiter : ILoginRateLimiter
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, AttemptState> _attempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<LoginRateLimiter> _logger;
    private DateTime _lastCleanupUtc = DateTime.UtcNow;

    public LoginRateLimiter(ILogger<LoginRateLimiter> logger)
    {
        _logger = logger;
    }

    public bool IsLockedOut(string? ipAddress, string? username, out TimeSpan remainingLockout)
    {
        remainingLockout = TimeSpan.Zero;
        CleanupIfNecessary();

        var now = DateTime.UtcNow;

        // Check IP-based lockout
        if (!string.IsNullOrWhiteSpace(ipAddress) && CheckLockout($"ip:{ipAddress.Trim()}", now, out remainingLockout))
        {
            return true;
        }

        // Check Username-based lockout
        if (!string.IsNullOrWhiteSpace(username) && CheckLockout($"user:{username.Trim().ToLowerInvariant()}", now, out remainingLockout))
        {
            return true;
        }

        return false;
    }

    public void RecordFailedAttempt(string? ipAddress, string? username)
    {
        CleanupIfNecessary();
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            RecordAttemptInternal($"ip:{ipAddress.Trim()}", now);
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            RecordAttemptInternal($"user:{username.Trim().ToLowerInvariant()}", now);
        }
    }

    public void ResetAttempts(string? ipAddress, string? username)
    {
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            _attempts.TryRemove($"ip:{ipAddress.Trim()}", out _);
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            _attempts.TryRemove($"user:{username.Trim().ToLowerInvariant()}", out _);
        }
    }

    private bool CheckLockout(string key, DateTime now, out TimeSpan remainingLockout)
    {
        remainingLockout = TimeSpan.Zero;

        if (_attempts.TryGetValue(key, out var state))
        {
            lock (state)
            {
                if (state.LockedUntilUtc.HasValue && state.LockedUntilUtc.Value > now)
                {
                    remainingLockout = state.LockedUntilUtc.Value - now;
                    return true;
                }
            }
        }

        return false;
    }

    private void RecordAttemptInternal(string key, DateTime now)
    {
        var state = _attempts.GetOrAdd(key, _ => new AttemptState { FirstAttemptUtc = now });

        lock (state)
        {
            // Reset if past attempt window
            if (now - state.FirstAttemptUtc > AttemptWindow)
            {
                state.FailedCount = 0;
                state.FirstAttemptUtc = now;
                state.LockedUntilUtc = null;
            }

            state.FailedCount++;

            if (state.FailedCount >= MaxFailedAttempts)
            {
                state.LockedUntilUtc = now + LockoutDuration;
                _logger.LogWarning("Brute-force lockout triggered for key '{Key}'. Locked for {Minutes} minutes.",
                    key, LockoutDuration.TotalMinutes);
            }
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

        foreach (var (key, state) in _attempts.ToArray())
        {
            lock (state)
            {
                var isExpired = (!state.LockedUntilUtc.HasValue || state.LockedUntilUtc.Value <= now)
                             && (now - state.FirstAttemptUtc > AttemptWindow);

                if (isExpired)
                {
                    _attempts.TryRemove(key, out _);
                }
            }
        }
    }

    private class AttemptState
    {
        public int FailedCount { get; set; }
        public DateTime FirstAttemptUtc { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
    }
}
