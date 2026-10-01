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
using CaseShop.Web.Services.Addresses;
using CaseShop.Web.Services.Payments;
using CaseShop.Web.Services.Email;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CaseShop.Web.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRateLimiter _orderRateLimiter;
    private readonly IVietnamAddressService _addressService;
    private readonly IPaymentService _paymentService;
    private readonly IOrderConfirmationEmailService _confirmationEmailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IOrderRateLimiter orderRateLimiter,
        IVietnamAddressService addressService,
        IPaymentService paymentService,
        IOrderConfirmationEmailService confirmationEmailService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _orderRateLimiter = orderRateLimiter;
        _addressService = addressService;
        _paymentService = paymentService;
        _confirmationEmailService = confirmationEmailService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<OrderDetailDto> CreateOrderAsync(CheckoutDto checkoutDto, CancellationToken cancellationToken = default)
    {
        if (checkoutDto == null)
        {
            throw new ArgumentNullException(nameof(checkoutDto));
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

        if (!new EmailAddressAttribute().IsValid(checkoutDto.Email))
        {
            throw new ArgumentException("Email không đúng định dạng.", nameof(checkoutDto.Email));
        }

        if (string.IsNullOrWhiteSpace(checkoutDto.AddressLine) || checkoutDto.ProvinceCode <= 0 || checkoutDto.WardCode <= 0)
        {
            throw new ArgumentException("Địa chỉ giao hàng chưa đầy đủ.", nameof(checkoutDto.AddressLine));
        }

        if (checkoutDto.Items == null || checkoutDto.Items.Count == 0)
        {
            throw new ArgumentException("Đơn hàng phải chứa ít nhất 1 sản phẩm.", nameof(checkoutDto.Items));
        }

        var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        if (_orderRateLimiter.IsRateLimited(clientIp, checkoutDto.Phone, out var retryAfter))
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            _logger.LogWarning("Order rate limit triggered for ClientIp {ClientIp}, Phone {Phone}. Retry after {Seconds}s",
                clientIp, checkoutDto.Phone, seconds);
            throw new InvalidOperationException($"Bạn đã gửi yêu cầu quá thường xuyên. Vui lòng đợi {seconds} giây trước khi gửi tiếp.");
        }

        _logger.LogInformation("Starting order creation for Phone {Phone}, ItemCount {ItemCount}, PaymentMethod {PaymentMethod}",
            checkoutDto.Phone, checkoutDto.Items.Count, checkoutDto.PaymentMethod);

        var provinces = await _addressService.GetProvincesAsync(cancellationToken);
        var province = provinces.FirstOrDefault(x => x.Code == checkoutDto.ProvinceCode)
            ?? throw new ArgumentException("Tỉnh/Thành phố không hợp lệ.");
        var wards = await _addressService.GetWardsAsync(province.Code, cancellationToken);
        var ward = wards.FirstOrDefault(x => x.Code == checkoutDto.WardCode)
            ?? throw new ArgumentException("Phường/Xã không thuộc Tỉnh/Thành phố đã chọn.");

        var productIds = checkoutDto.Items.Select(x => x.ProductId).Distinct().ToArray();
        var products = (await _productRepository.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(x => x.Id);

        var orderId = Guid.NewGuid();
        var orderCode = await GenerateUniqueOrderCodeAsync(cancellationToken);

        var order = new Order
        {
            Id = orderId,
            OrderCode = orderCode,
            CustomerName = checkoutDto.CustomerName.Trim(),
            Phone = checkoutDto.Phone.Trim(),
            Email = checkoutDto.Email.Trim(),
            ProvinceCode = province.Code,
            ProvinceName = province.Name,
            WardCode = ward.Code,
            WardName = ward.Name,
            AddressLine = checkoutDto.AddressLine.Trim(),
            Address = $"{checkoutDto.AddressLine.Trim()}, {ward.Name}, {province.Name}",
            PaymentMethod = Enum.IsDefined(typeof(PaymentMethodType), checkoutDto.PaymentMethod) ? checkoutDto.PaymentMethod : throw new ArgumentException("Phuong thuc thanh toan khong hop le."),
            PaymentStatus = checkoutDto.PaymentMethod == PaymentMethodType.PayOSQr ? PaymentStatus.Pending : PaymentStatus.NotRequired,
            ConfirmationEmailStatus = EmailDeliveryStatus.Pending,
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

            if (!products.TryGetValue(itemDto.ProductId, out var product) || !product.IsActive)
            {
                throw new InvalidOperationException($"Sản phẩm với mã {itemDto.ProductId} không tồn tại hoặc đã ngừng kinh doanh.");
            }

            var orderItemId = Guid.NewGuid();
            var orderItem = new OrderItem
            {
                Id = orderItemId,
                OrderId = orderId,
                ProductId = product.Id,
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

        order.SubtotalAmount = totalAmount;
        order.ShippingFee = 0m;
        order.TotalAmount = order.SubtotalAmount + order.ShippingFee;

        await _orderRepository.AddAsync(order, cancellationToken);

        // Record order to rate limit future requests
        _orderRateLimiter.RecordOrder(clientIp, checkoutDto.Phone);

        if (order.PaymentMethod == PaymentMethodType.PayOSQr)
        {
            try
            {
                await _paymentService.CreatePayOsPaymentAsync(order.Id, cancellationToken);
                order = await _orderRepository.GetForUpdateByIdAsync(order.Id, cancellationToken) ?? order;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Order {OrderId} persisted but PayOS QR could not be created", order.Id);
            }
        }

        _logger.LogInformation("Order creation completed for OrderId {OrderId}, OrderCode {OrderCode}, TotalAmount {TotalAmount}",
            order.Id, order.OrderCode, order.TotalAmount);

        var persistedOrder = await _orderRepository.GetByIdAsync(order.Id, cancellationToken) ?? order;
        return MapToDetailDto(persistedOrder);
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

    public Task<bool> RetryConfirmationEmailAsync(Guid orderId, CancellationToken cancellationToken = default)
        => _confirmationEmailService.SendAsync(orderId, allowRetry: true, cancellationToken);

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
            ProvinceCode = order.ProvinceCode,
            ProvinceName = order.ProvinceName,
            WardCode = order.WardCode,
            WardName = order.WardName,
            AddressLine = order.AddressLine,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            Status = order.Status,
            SubtotalAmount = order.SubtotalAmount,
            ShippingFee = order.ShippingFee,
            TotalAmount = order.TotalAmount,
            ConfirmationEmailStatus = order.ConfirmationEmailStatus,
            Payment = order.PaymentMethod == PaymentMethodType.PayOSQr ? new PaymentInfoDto
            {
                Status = order.PaymentStatus,
                ProviderOrderCode = order.PayOsOrderCode,
                CheckoutUrl = order.PayOsCheckoutUrl,
                QrCode = order.PayOsQrCode,
                ExpiresAt = order.PaymentExpiresAt,
                PaidAt = order.PaidAt
            } : null,
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


