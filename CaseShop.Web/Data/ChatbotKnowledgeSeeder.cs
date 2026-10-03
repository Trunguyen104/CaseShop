using CaseShop.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CaseShop.Web.Data;

public static class ChatbotKnowledgeSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        var entries = CreateEntries();
        var existingEntryList = await context.ChatbotKnowledgeEntries
            .ToListAsync(cancellationToken);
        var existingEntries = existingEntryList.ToDictionary(entry => entry.IntentCode, StringComparer.Ordinal);

        var addedCount = 0;
        var updatedCount = 0;
        foreach (var preparedEntry in entries)
        {
            if (!existingEntries.TryGetValue(preparedEntry.IntentCode, out var existingEntry))
            {
                await context.ChatbotKnowledgeEntries.AddAsync(preparedEntry, cancellationToken);
                addedCount++;
                continue;
            }

            if (ApplyPreparedContent(existingEntry, preparedEntry))
            {
                existingEntry.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }
        }

        if (addedCount == 0 && updatedCount == 0)
        {
            logger.LogInformation("Chatbot knowledge seed is already up to date");
            return;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Synchronized chatbot knowledge with {AddedCount} added and {UpdatedCount} updated entries",
            addedCount,
            updatedCount);
    }

    private static ChatbotKnowledgeEntry[] CreateEntries()
    {
        var createdAt = DateTime.UtcNow;
        return
        [
            Create("customize_start", ChatbotCategory.Customization,
                "Làm thế nào để tự thiết kế ốp lưng?",
                "Bạn hãy chọn dòng điện thoại và mẫu ốp, sau đó tải ảnh, thêm sticker hoặc văn bản trong trình thiết kế. Khi hoàn tất, chọn xem trước và thêm thiết kế vào giỏ hàng.",
                "thiết kế ốp lưng;thiết kế;custom;tạo ốp;tùy chỉnh ốp;làm ốp riêng", "/customize", "Bắt đầu thiết kế", 100, createdAt),

            Create("upload_image", ChatbotCategory.Customization,
                "Làm thế nào để tải ảnh lên thiết kế?",
                "Trong trình thiết kế, chọn công cụ tải ảnh rồi chọn ảnh JPG, PNG hoặc WEBP từ thiết bị. Bạn có thể di chuyển, thay đổi kích thước và cắt ảnh sau khi thêm vào canvas.",
                "tải ảnh;upload ảnh;thêm ảnh;đưa ảnh lên;hình của tôi", "/customize", "Mở trình thiết kế", 90, createdAt),

            Create("add_text", ChatbotCategory.Customization,
                "Làm thế nào để thêm chữ vào ốp lưng?",
                "Trong trình thiết kế, mở mục Văn bản, nhập nội dung rồi thêm vào canvas. Sau đó bạn có thể đổi phông chữ, màu sắc, kích thước, căn chỉnh và xoay văn bản.",
                "thêm chữ;văn bản;font chữ;phông chữ;đổi chữ", "/customize", "Thêm văn bản", 85, createdAt),

            Create("product_price", ChatbotCategory.Product,
                "Ốp lưng có giá bao nhiêu?",
                "Giá được hiển thị trực tiếp trên từng sản phẩm và có thể thay đổi theo mẫu ốp. Bạn hãy mở cửa hàng để xem mức giá hiện tại trước khi đặt hàng.",
                "giá;bao nhiêu tiền;giá ốp;giá sản phẩm;chi phí", "/shop", "Xem sản phẩm", 95, createdAt),

            Create("payment_methods", ChatbotCategory.Payment,
                "ME-ISM hỗ trợ những phương thức thanh toán nào?",
                "ME-ISM hỗ trợ thanh toán khi nhận hàng và thanh toán QR qua PayOS. Với QR, đơn hàng chỉ được xác nhận sau khi PayOS thông báo giao dịch thành công.",
                "thanh toán;cod;payos;mã qr;quét qr;chuyển khoản;thanh toán khi nhận hàng;qr hết hạn;chưa cập nhật thanh toán", "/checkout", "Đi đến thanh toán", 95, createdAt),

            Create("shipping_time", ChatbotCategory.Shipping,
                "Đơn hàng được giao trong bao lâu?",
                "Thời gian giao hàng phụ thuộc vào địa chỉ nhận hàng và thời gian hoàn thiện sản phẩm. Thông tin cụ thể sẽ được ME-ISM xác nhận theo đơn hàng của bạn.",
                "giao hàng;giao hàng bao lâu;thời gian giao;vận chuyển;mấy ngày;bao giờ nhận;bao lâu có hàng;khi nào nhận", null, null, 80, createdAt),

            Create("shipping_fee", ChatbotCategory.Shipping,
                "Phí vận chuyển là bao nhiêu?",
                "Phí vận chuyển được hệ thống hiển thị trong phần tổng kết đơn hàng trước khi bạn xác nhận đặt hàng.",
                "phí vận chuyển;phí ship;tiền ship;ship bao nhiêu;freeship;miễn phí giao hàng", "/checkout", "Kiểm tra đơn hàng", 82, createdAt),

            Create("track_order", ChatbotCategory.OrderTracking,
                "Làm thế nào để theo dõi đơn hàng?",
                "Bạn có thể mở trang Theo dõi đơn hàng và nhập thông tin được yêu cầu. Vì lý do bảo mật, chatbot không hiển thị trực tiếp thông tin đơn hàng.",
                "theo dõi đơn;kiểm tra đơn;đơn của tôi;đơn hàng của tôi;trạng thái đơn;tra cứu đơn;đơn tới đâu;đơn hàng tới đâu", "/track", "Theo dõi đơn hàng", 100, createdAt),

            Create("return_policy", ChatbotCategory.ReturnAndWarranty,
                "Tôi có thể đổi trả sản phẩm không?",
                "Sản phẩm cá nhân hóa cần được kiểm tra theo tình trạng thực tế và chính sách hiện hành. Bạn hãy liên hệ ME-ISM, cung cấp mã đơn cùng hình ảnh sản phẩm để được hướng dẫn.",
                "đổi trả;trả hàng;hoàn hàng;đổi sản phẩm;hoàn tiền", null, null, 90, createdAt),

            Create("warranty_policy", ChatbotCategory.ReturnAndWarranty,
                "Sản phẩm có được bảo hành không?",
                "Nếu sản phẩm có lỗi in ấn hoặc lỗi hoàn thiện, bạn hãy liên hệ ME-ISM và cung cấp mã đơn cùng hình ảnh để đội ngũ hỗ trợ kiểm tra điều kiện bảo hành.",
                "bảo hành;lỗi in;bong tróc;lỗi sản phẩm;ốp bị lỗi", null, null, 90, createdAt),

            Create("editor_crop", ChatbotCategory.Troubleshooting,
                "Làm thế nào để cắt ảnh trong trình thiết kế?",
                "Hãy chọn ảnh trên canvas rồi chọn Cắt ảnh trong bảng thuộc tính. Bạn có thể kéo các cạnh của khung cắt, di chuyển vùng ảnh và xác nhận khi bố cục đã phù hợp.",
                "cắt ảnh;crop ảnh;chỉnh ảnh;khung cắt", "/customize", "Mở trình thiết kế", 85, createdAt),

            Create("contact_support", ChatbotCategory.Contact,
                "Tôi muốn liên hệ ME-ISM",
                "Bạn có thể sử dụng thông tin liên hệ ở cuối trang hoặc email hỗ trợ của ME-ISM. Khi cần hỗ trợ đơn hàng, vui lòng chuẩn bị mã đơn để được xử lý nhanh hơn.",
                "liên hệ;hỗ trợ;email;hotline;nhân viên;tư vấn", null, null, 75, createdAt)
        ];
    }

    private static ChatbotKnowledgeEntry Create(
        string intentCode,
        ChatbotCategory category,
        string question,
        string answer,
        string keywords,
        string? actionUrl,
        string? actionLabel,
        int priority,
        DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        IntentCode = intentCode,
        Category = category,
        Question = question,
        Answer = answer,
        Keywords = keywords,
        ActionType = actionUrl is null ? ChatbotActionType.None : ChatbotActionType.Link,
        ActionUrl = actionUrl,
        ActionLabel = actionLabel,
        Priority = priority,
        IsActive = true,
        CreatedAt = createdAt
    };

    private static bool ApplyPreparedContent(ChatbotKnowledgeEntry target, ChatbotKnowledgeEntry source)
    {
        var changed = target.Category != source.Category ||
                      target.Question != source.Question ||
                      target.Answer != source.Answer ||
                      target.Keywords != source.Keywords ||
                      target.ActionType != source.ActionType ||
                      target.ActionLabel != source.ActionLabel ||
                      target.ActionUrl != source.ActionUrl ||
                      target.Priority != source.Priority;

        if (!changed)
        {
            return false;
        }

        target.Category = source.Category;
        target.Question = source.Question;
        target.Answer = source.Answer;
        target.Keywords = source.Keywords;
        target.ActionType = source.ActionType;
        target.ActionLabel = source.ActionLabel;
        target.ActionUrl = source.ActionUrl;
        target.Priority = source.Priority;
        return true;
    }
}
