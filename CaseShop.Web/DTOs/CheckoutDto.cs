using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CaseShop.Web.Entities;

namespace CaseShop.Web.DTOs;

public class CheckoutDto
{
    [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên tối đa 100 ký tự")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc")]
    [MaxLength(200, ErrorMessage = "Địa chỉ chi tiết tối đa 200 ký tự")]
    public string AddressLine { get; set; } = string.Empty;

    // Transitional alias for the existing checkout component.
    public string Address
    {
        get => AddressLine;
        set => AddressLine = value;
    }

    [Range(1, int.MaxValue, ErrorMessage = "Tỉnh/Thành phố là bắt buộc")]
    public int ProvinceCode { get; set; }

    [Required(ErrorMessage = "Tên Tỉnh/Thành phố là bắt buộc")]
    [MaxLength(100)]
    public string ProvinceName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Phường/Xã là bắt buộc")]
    public int WardCode { get; set; }

    [Required(ErrorMessage = "Tên Phường/Xã là bắt buộc")]
    [MaxLength(100)]
    public string WardName { get; set; } = string.Empty;

    [EnumDataType(typeof(PaymentMethodType), ErrorMessage = "Phuong thuc thanh toan khong hop le")]
    public PaymentMethodType PaymentMethod { get; set; } = PaymentMethodType.CashOnDelivery;

    [MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất 1 sản phẩm")]
    public List<CheckoutItemDto> Items { get; set; } = new();
}

public class CheckoutItemDto
{
    [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
    public Guid ProductId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
    public int Quantity { get; set; } = 1;

    public string? Variant { get; set; }

    public CustomDesignInputDto? CustomDesign { get; set; }
}

public class CustomDesignInputDto
{
    [Required(ErrorMessage = "Ảnh preview thiết kế là bắt buộc")]
    public string PreviewImageUrl { get; set; } = string.Empty;

    public string? OriginalImageUrl { get; set; }

    [Required(ErrorMessage = "Dữ liệu thiết kế canvas là bắt buộc")]
    public string DesignData { get; set; } = string.Empty;
}

