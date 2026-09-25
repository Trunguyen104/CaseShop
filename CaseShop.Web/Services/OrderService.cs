using System;
using System.Collections.Generic;
using System.Linq;
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

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRateLimiter _orderRateLimiter;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IOrderRateLimiter orderRateLimiter,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _orderRateLimiter = orderRateLimiter;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<OrderDetailDto> CreateOrderAsync(CheckoutDto checkoutDto, CancellationToken cancellationToken = default)
    {
        if (checkoutDto == null)
        {
            throw new ArgumentNullException(nameof(checkoutDto));
        }

        var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        // Anti-spam / DoS rate limit check
        if (_orderRateLimiter.IsRateLimited(clientIp, checkoutDto.Phone, out var retryAfter))
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            _logger.LogWarning("Order rate limit triggered for Client IP '{ClientIp}', Phone '{Phone}'. Retry after {Seconds}s.",
                clientIp, checkoutDto.Phone, seconds);
            throw new InvalidOperationException($"Bạn đã gửi yêu cầu quá thường xuyên. Để đảm bảo hệ thống không bị quá tải, vui lòng đợi {seconds} giây trước khi gửi tiếp.");
        }

        if (string.IsNullOrWhiteSpace(checkoutDto.CustomerName))
        {
            throw new ArgumentException("Tên khách hàng không được để trống.", nameof(checkoutDto.CustomerName));
        }

        if (string.IsNullOrWhiteSpace(checkoutDto.Phone))
        {
            throw new ArgumentException("Số điện thoại không được để trống.", nameof(checkoutDto.Phone));
        }

        if (string.IsNullOrWhiteSpace(checkoutDto.Email))
        {
            throw new ArgumentException("Email không được để trống.", nameof(checkoutDto.Email));
        }

        if (string.IsNullOrWhiteSpace(checkoutDto.Address))
        {
            throw new ArgumentException("Địa chỉ không được để trống.", nameof(checkoutDto.Address));
        }

        if (checkoutDto.Items == null || checkoutDto.Items.Count == 0)
        {
            throw new ArgumentException("Đơn hàng phải chứa ít nhất 1 sản phẩm.", nameof(checkoutDto.Items));
        }

        var orderId = Guid.NewGuid();
        var orderCode = await GenerateUniqueOrderCodeAsync(cancellationToken);

        var order = new Order
        {
            Id = orderId,
            OrderCode = orderCode,
            CustomerName = checkoutDto.CustomerName.Trim(),
            Phone = checkoutDto.Phone.Trim(),
            Email = checkoutDto.Email.Trim(),
            Address = checkoutDto.Address.Trim(),
            PaymentMethod = Enum.IsDefined(typeof(PaymentMethodType), checkoutDto.PaymentMethod) ? checkoutDto.PaymentMethod : throw new ArgumentException("Phuong thuc thanh toan khong hop le."),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        decimal totalAmount = 0;

        foreach (var itemDto in checkoutDto.Items)
        {
            if (itemDto.Quantity <= 0 || itemDto.Quantity > 100)
            {
                throw new ArgumentException("So luong san pham phai tu 1 den 100.");
            }

            var product = await _productRepository.GetByIdAsync(itemDto.ProductId, cancellationToken);
            if (product == null || !product.IsActive)
            {
                throw new InvalidOperationException($"Sản phẩm với mã {itemDto.ProductId} không tồn tại hoặc đã ngừng kinh doanh.");
            }

            var orderItemId = Guid.NewGuid();
            var orderItem = new OrderItem
            {
                Id = orderItemId,
                OrderId = orderId,
                ProductId = product.Id,
                Product = product,
                Quantity = itemDto.Quantity,
                UnitPrice = product.Price,
                Variant = itemDto.Variant
            };

            if (itemDto.CustomDesign != null)
            {
                if (string.IsNullOrWhiteSpace(itemDto.CustomDesign.PreviewImageUrl))
                {
                    throw new ArgumentException("Ảnh thiết kế không được để trống.");
                }

                if (string.IsNullOrWhiteSpace(itemDto.CustomDesign.DesignData))
                {
                    throw new ArgumentException("Dữ liệu thiết kế không được để trống.");
                }

                var customDesign = new CustomDesign
                {
                    Id = Guid.NewGuid(),
                    OrderItemId = orderItemId,
                    PreviewImageUrl = itemDto.CustomDesign.PreviewImageUrl,
                    OriginalImageUrl = itemDto.CustomDesign.OriginalImageUrl,
                    DesignData = itemDto.CustomDesign.DesignData,
                    CreatedAt = DateTime.UtcNow
                };

                orderItem.CustomDesign = customDesign;
            }

            totalAmount += orderItem.UnitPrice * orderItem.Quantity;
            order.Items.Add(orderItem);
        }

        order.TotalAmount = totalAmount;

        await _orderRepository.AddAsync(order, cancellationToken);

        // Record order to rate limit future requests
        _orderRateLimiter.RecordOrder(clientIp, checkoutDto.Phone);

        return MapToDetailDto(order);
    }

    public async Task<OrderTrackingDto?> TrackOrderByCodeAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return null;
        }

        var order = await _orderRepository.GetByOrderCodeAsync(orderCode.Trim().ToUpperInvariant(), cancellationToken);
        if (order == null)
        {
            return null;
        }

        return new OrderTrackingDto
        {
            OrderCode = order.OrderCode,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            TotalAmount = order.TotalAmount,
            ItemCount = order.Items.Sum(i => i.Quantity),
            Items = order.Items.Select(i => new OrderTrackingItemDto
            {
                ProductName = i.Product?.Name ?? "Sản phẩm",
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                PreviewImageUrl = i.CustomDesign?.PreviewImageUrl ?? i.Product?.ImageUrl
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(OrderStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(status, searchTerm, cancellationToken);
        return orders.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderCode = o.OrderCode,
            CustomerName = o.CustomerName,
            Phone = o.Phone,
            PaymentMethod = o.PaymentMethod,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            TotalItems = o.Items.Sum(i => i.Quantity),
            CreatedAt = o.CreatedAt
        }).ToList();
    }

    public async Task<OrderDetailDto?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        return order == null ? null : MapToDetailDto(order);
    }

    public async Task<OrderDetailDto?> GetOrderByCodeAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return null;
        }

        var order = await _orderRepository.GetByOrderCodeAsync(orderCode.Trim().ToUpperInvariant(), cancellationToken);
        return order == null ? null : MapToDetailDto(order);
    }

    public async Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            return false;
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepository.UpdateAsync(order, cancellationToken);
        return true;
    }

    private async Task<string> GenerateUniqueOrderCodeAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < 10; i++)
        {
            var datePart = DateTime.UtcNow.ToString("yyMMdd");
            var randomPart = RandomNumberGenerator.GetHexString(4).ToUpperInvariant();
            var code = $"CS-{datePart}-{randomPart}";

            if (!await _orderRepository.ExistsByOrderCodeAsync(code, cancellationToken))
            {
                return code;
            }
        }

        return $"CS-{DateTime.UtcNow.Ticks:X}";
    }

    private static OrderDetailDto MapToDetailDto(Order order)
    {
        return new OrderDetailDto
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            CustomerName = order.CustomerName,
            Phone = order.Phone,
            Email = order.Email,
            Address = order.Address,
            PaymentMethod = order.PaymentMethod,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(i => new OrderItemDetailDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                ProductDescription = i.Product?.Description,
                Variant = i.Variant,
                ProductImageUrl = i.Product?.ImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                CustomDesign = i.CustomDesign == null ? null : new CustomDesignDto
                {
                    Id = i.CustomDesign.Id,
                    PreviewImageUrl = i.CustomDesign.PreviewImageUrl,
                    OriginalImageUrl = i.CustomDesign.OriginalImageUrl,
                    DesignData = i.CustomDesign.DesignData,
                    CreatedAt = i.CustomDesign.CreatedAt
                }
            }).ToList()
        };
    }
}


