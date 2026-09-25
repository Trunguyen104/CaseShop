using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Repositories;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    Task<bool> ExistsByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}

