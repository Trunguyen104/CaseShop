using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Services;

public interface IOrderService
{
    Task<OrderDetailDto> CreateOrderAsync(CheckoutDto checkoutDto, CancellationToken cancellationToken = default);
    Task<OrderTrackingDto?> TrackOrderByCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(OrderStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<OrderDetailDto?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderDetailDto?> GetOrderByCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default);
}

