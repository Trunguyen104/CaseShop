using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    }

