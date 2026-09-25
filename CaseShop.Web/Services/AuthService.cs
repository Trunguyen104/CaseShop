using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;
using CaseShop.Web.Repositories;
using CaseShop.Web.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CaseShop.Web.Services;

public class AuthService : IAuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private readonly IAdminRepository _adminRepository;
    private readonly ILoginRateLimiter _loginRateLimiter;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAdminRepository adminRepository,
        ILoginRateLimiter loginRateLimiter,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _adminRepository = adminRepository;
        _loginRateLimiter = loginRateLimiter;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<(bool Succeeded, Admin? Admin, string? ErrorMessage)> ValidateCredentialsAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
        {
            return (false, null, "Tên đăng nhập và mật khẩu không được để trống.");
        }

        var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        // 1. Check if user or IP is currently locked out
        if (_loginRateLimiter.IsLockedOut(clientIp, loginDto.Username, out var remainingLockout))
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(remainingLockout.TotalMinutes));
            _logger.LogWarning("Blocked login attempt due to active lockout: User '{Username}', IP '{ClientIp}'",
                loginDto.Username, clientIp);
            return (false, null, $"Bạn đã nhập sai quá nhiều lần. Để đảm bảo an toàn, vui lòng thử lại sau {minutes} phút.");
        }

        var admin = await _adminRepository.GetByUsernameAsync(loginDto.Username.Trim(), cancellationToken);
        if (admin == null || !VerifyPassword(admin.PasswordHash, loginDto.Password))
        {
            // Record failed attempt
            _loginRateLimiter.RecordFailedAttempt(clientIp, loginDto.Username);
            _logger.LogWarning("Failed login attempt for user '{Username}' from IP '{ClientIp}'",
                loginDto.Username, clientIp);
            return (false, null, "Tài khoản hoặc mật khẩu không chính xác.");
        }

        // 2. Successful login - clear any previous failed attempt counts
        _loginRateLimiter.ResetAttempts(clientIp, loginDto.Username);
        _logger.LogInformation("Successful admin login for user '{Username}' from IP '{ClientIp}'",
            loginDto.Username, clientIp);

        return (true, admin, null);
    }

    public async Task EnsureDefaultAdminCreatedAsync(string defaultUsername, string defaultPassword, CancellationToken cancellationToken = default)
    {
        var hasAdmin = await _adminRepository.AnyAsync(cancellationToken);
        if (!hasAdmin)
        {
            var admin = new Admin
            {
                Id = Guid.NewGuid(),
                Username = defaultUsername.Trim(),
                PasswordHash = HashPassword(defaultPassword),
                CreatedAt = DateTime.UtcNow
            };

            await _adminRepository.AddAsync(admin, cancellationToken);
        }
    }

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var parts = hashedPassword.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                providedPassword,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }
}
