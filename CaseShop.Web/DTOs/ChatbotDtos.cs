using System.ComponentModel.DataAnnotations;
using CaseShop.Web.Entities;

namespace CaseShop.Web.DTOs;

public sealed class ChatbotRequestDto
{
    [Required(ErrorMessage = "Vui lòng nhập câu hỏi.")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Câu hỏi phải có từ 1 đến 500 ký tự.")]
    public string Message { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Đường dẫn hiện tại tối đa 200 ký tự.")]
    public string? CurrentRoute { get; set; }
}

public sealed class ChatbotActionDto
{
    public ChatbotActionType Type { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

public sealed class ChatbotResponseDto
{
    public string IntentCode { get; init; } = string.Empty;
    public ChatbotCategory Category { get; init; }
    public string Answer { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public bool IsFallback { get; init; }
    public ChatbotActionDto? Action { get; init; }
    public IReadOnlyList<string> QuickReplies { get; init; } = Array.Empty<string>();
}
