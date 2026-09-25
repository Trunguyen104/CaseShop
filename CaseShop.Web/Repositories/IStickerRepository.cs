using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Repositories;

public interface IStickerRepository
{
    Task<IReadOnlyList<Sticker>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<Sticker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Sticker sticker, CancellationToken cancellationToken = default);
    Task UpdateAsync(Sticker sticker, CancellationToken cancellationToken = default);
    }

