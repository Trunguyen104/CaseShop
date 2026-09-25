using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CaseShop.Web.Data;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _context;

    public AdminRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Admin?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Admins.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Admin?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Admins.FirstOrDefaultAsync(a => a.Username == username, cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Admins.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Admin admin, CancellationToken cancellationToken = default)
    {
        await _context.Admins.AddAsync(admin, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Admin admin, CancellationToken cancellationToken = default)
    {
        _context.Admins.Update(admin);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
