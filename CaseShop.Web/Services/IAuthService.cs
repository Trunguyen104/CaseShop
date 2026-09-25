using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Services;

public interface IAuthService
{
    Task<(bool Succeeded, Admin? Admin, string? ErrorMessage)> ValidateCredentialsAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
    Task EnsureDefaultAdminCreatedAsync(string defaultUsername, string defaultPassword, CancellationToken cancellationToken = default);
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
