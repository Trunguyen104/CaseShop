using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CaseShop.Web.Data;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Repositories;

public class StickerRepository : IStickerRepository
{
    private readonly AppDbContext _context;

    public StickerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Sticker>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Stickers.AsNoTracking().AsQueryable();
        
        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Sticker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Stickers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(Sticker sticker, CancellationToken cancellationToken = default)
    {
        _context.Stickers.Add(sticker);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Sticker sticker, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(sticker).State == EntityState.Detached)
        {
            _context.Stickers.Update(sticker);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}
