using System;

namespace CaseShop.Web.Services.Security;

/// <summary>
/// Service to protect admin authentication against brute-force and credential stuffing attacks.
/// </summary>
public interface ILoginRateLimiter
{
    /// <summary>
    /// Checks if the IP address or username is currently locked out.
    /// </summary>
    bool IsLockedOut(string? ipAddress, string? username, out TimeSpan remainingLockout);

    /// <summary>
    /// Records a failed authentication attempt for both the client IP and username.
    /// </summary>
    void RecordFailedAttempt(string? ipAddress, string? username);

    /// <summary>
    /// Resets the failed attempts counter upon successful login.
    /// </summary>
    void ResetAttempts(string? ipAddress, string? username);
}
