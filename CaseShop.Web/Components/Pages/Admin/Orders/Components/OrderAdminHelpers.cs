using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Components.Pages.Admin.Orders.Components;

public static class OrderAdminHelpers
{
    public static string? GetItemThumbnail(OrderItemDetailDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.CustomDesign?.PreviewImageUrl))
        {
            return item.CustomDesign.PreviewImageUrl;
        }
        if (!string.IsNullOrWhiteSpace(item.ProductImageUrl))
        {
            return item.ProductImageUrl;
        }
        return null;
    }

    public static string FormatShortId(Guid id)
    {
        var str = id.ToString();
        if (str.Length >= 8)
        {
            return $"{str.Substring(0, 4)}...{str.Substring(str.Length - 4)}";
        }
        return str;
    }

    public static string? GetPhoneModel(OrderItemDetailDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.Variant))
        {
            return item.Variant.Trim();
        }

        if (item.CustomDesign != null && !string.IsNullOrWhiteSpace(item.CustomDesign.DesignData))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(item.CustomDesign.DesignData);
                if (doc.RootElement.TryGetProperty("phoneModelName", out var modelProp) && !string.IsNullOrWhiteSpace(modelProp.GetString()))
                {
                    return modelProp.GetString();
                }
            }
            catch { }
        }
        return null;
    }

    public static string GetStatusDisplayName(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Chờ xử lý",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Producing => "Đang in / gia công",
        OrderStatus.Shipping => "Đang giao hàng",
        OrderStatus.Completed => "Hoàn tất",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    public static string GetStatusBadgeStyle(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 border border-amber-300/50",
        OrderStatus.Confirmed => "bg-sky-100 dark:bg-sky-950/60 text-sky-800 dark:text-sky-300 border border-sky-300/50",
        OrderStatus.Producing => "bg-purple-100 dark:bg-purple-950/60 text-purple-800 dark:text-purple-300 border border-purple-300/50",
        OrderStatus.Shipping => "bg-indigo-100 dark:bg-indigo-950/60 text-indigo-800 dark:text-indigo-300 border border-indigo-300/50",
        OrderStatus.Completed => "bg-emerald-100 dark:bg-emerald-950/60 text-emerald-800 dark:text-emerald-300 border border-emerald-300/50",
        OrderStatus.Cancelled => "bg-rose-100 dark:bg-rose-950/60 text-rose-800 dark:text-rose-300 border border-rose-300/50",
        _ => "bg-surface-container text-on-surface"
    };

    public static string GetStatusDotColor(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-amber-500",
        OrderStatus.Confirmed => "bg-sky-500",
        OrderStatus.Producing => "bg-purple-500",
        OrderStatus.Shipping => "bg-indigo-500",
        OrderStatus.Completed => "bg-emerald-500",
        OrderStatus.Cancelled => "bg-rose-500",
        _ => "bg-outline"
    };

    public static string GetPaymentMethodDisplayName(PaymentMethodType method) => method switch
    {
        PaymentMethodType.CashOnDelivery => "Thanh toán khi nhận hàng (COD)",
        PaymentMethodType.BankTransfer => "Chuyển khoản ngân hàng (VietQR)",
        _ => method.ToString()
    };
}
