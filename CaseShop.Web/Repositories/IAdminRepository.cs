using System;
using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Repositories;

public interface IAdminRepository
{
    Task<Admin?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Admin?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Admin admin, CancellationToken cancellationToken = default);
    Task UpdateAsync(Admin admin, CancellationToken cancellationToken = default);
}
