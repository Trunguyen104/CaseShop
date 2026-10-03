using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;
using CaseShop.Web.Repositories;
using CaseShop.Web.Services.Security;
using Microsoft.Extensions.Caching.Memory;

namespace CaseShop.Web.Services;

public sealed class ChatbotService : IChatbotService
{
    private const string KnowledgeCacheKey = "chatbot:knowledge:active:v1";
    private const int MatchThreshold = 10;
    private static readonly TimeSpan KnowledgeCacheDuration = TimeSpan.FromMinutes(10);
    private static readonly string[] DefaultQuickReplies =
    [
        "Thiết kế ốp lưng",
        "Giá và thanh toán",
        "Giao hàng",
        "Theo dõi đơn",
        "Đổi trả và bảo hành"
    ];

    private readonly IChatbotKnowledgeRepository _repository;
    private readonly IChatbotRateLimiter _rateLimiter;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(
        IChatbotKnowledgeRepository repository,
        IChatbotRateLimiter rateLimiter,
        IMemoryCache cache,
        ILogger<ChatbotService> logger)
    {
        _repository = repository;
        _rateLimiter = rateLimiter;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ChatbotResponseDto> GetResponseAsync(
        ChatbotRequestDto request,
        string clientKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var rateLimitKey = HashClientKey(clientKey);
        if (!_rateLimiter.TryAcquire(rateLimitKey, out var retryAfter))
        {
            _logger.LogWarning("Chatbot rate limit reached. Retry after {RetryAfterSeconds} seconds", Math.Ceiling(retryAfter.TotalSeconds));
            throw new InvalidOperationException("Bạn đang gửi câu hỏi quá nhanh. Vui lòng thử lại sau ít phút.");
        }

        _logger.LogInformation("Processing chatbot question with length {MessageLength}", request.Message.Trim().Length);

        try
        {
            var entries = await GetKnowledgeEntriesAsync(cancellationToken);
            var normalizedMessage = NormalizeText(request.Message);
            var routeCategory = ResolveRouteCategory(request.CurrentRoute);

            var bestMatch = entries
                .Select(entry => new MatchResult(entry, CalculateScore(entry, normalizedMessage, routeCategory)))
                .Where(result => result.Score >= MatchThreshold)
                .OrderByDescending(result => result.Score)
                .ThenByDescending(result => result.Entry.Priority)
                .ThenBy(result => result.Entry.IntentCode)
                .FirstOrDefault();

            if (bestMatch is null)
            {
                _logger.LogInformation("No chatbot intent matched the submitted question");
                return CreateFallbackResponse();
            }

            _logger.LogInformation("Chatbot matched intent {IntentCode} with score {MatchScore}", bestMatch.Entry.IntentCode, bestMatch.Score);
            return MapResponse(bestMatch);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve chatbot response");
            throw new InvalidOperationException("Chatbot đang tạm thời không thể trả lời. Vui lòng thử lại sau.");
        }
    }

    private async Task<IReadOnlyList<ChatbotKnowledgeEntry>> GetKnowledgeEntriesAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(KnowledgeCacheKey, out IReadOnlyList<ChatbotKnowledgeEntry>? cachedEntries) && cachedEntries is not null)
        {
            return cachedEntries;
        }

        var entries = await _repository.GetActiveAsync(cancellationToken);
        _cache.Set(KnowledgeCacheKey, entries, KnowledgeCacheDuration);
        return entries;
    }

    private static void ValidateRequest(ChatbotRequestDto request)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);
        if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            throw new ArgumentException(validationResults[0].ErrorMessage ?? "Câu hỏi không hợp lệ.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Vui lòng nhập câu hỏi.", nameof(request));
        }
    }

    private static int CalculateScore(
        ChatbotKnowledgeEntry entry,
        string normalizedMessage,
        ChatbotCategory? routeCategory)
    {
        var normalizedQuestion = NormalizeText(entry.Question);
        if (normalizedMessage == normalizedQuestion)
        {
            return 100 + entry.Priority;
        }

        var lexicalScore = 0;
        if (normalizedQuestion.Length >= 8 && normalizedMessage.Contains(normalizedQuestion, StringComparison.Ordinal))
        {
            lexicalScore += 30;
        }

        var keywords = entry.Keywords.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeText)
            .Where(keyword => keyword.Length >= 2)
            .Distinct(StringComparer.Ordinal);

        foreach (var keyword in keywords)
        {
            if (ContainsPhrase(normalizedMessage, keyword))
            {
                lexicalScore += keyword.Contains(' ') ? 15 : 10;
            }
        }

        // Context and priority only rank genuine text matches; they must not create a match.
        if (lexicalScore == 0)
        {
            return 0;
        }

        var score = lexicalScore;

        if (routeCategory == entry.Category)
        {
            score += 5;
        }

        return score + Math.Min(entry.Priority, 100) / 10;
    }

    private static bool ContainsPhrase(string normalizedMessage, string keyword)
    {
        var paddedMessage = $" {normalizedMessage} ";
        var paddedKeyword = $" {keyword} ";
        return paddedMessage.Contains(paddedKeyword, StringComparison.Ordinal);
    }

    private static ChatbotResponseDto MapResponse(MatchResult result)
    {
        var entry = result.Entry;
        ChatbotActionDto? action = null;

        if (entry.ActionType == ChatbotActionType.Link &&
            !string.IsNullOrWhiteSpace(entry.ActionLabel) &&
            IsSafeInternalUrl(entry.ActionUrl))
        {
            action = new ChatbotActionDto
            {
                Type = ChatbotActionType.Link,
                Label = entry.ActionLabel,
                Url = entry.ActionUrl!
            };
        }

        return new ChatbotResponseDto
        {
            IntentCode = entry.IntentCode,
            Category = entry.Category,
            Answer = entry.Answer,
            Confidence = Math.Min(1m, result.Score / 100m),
            IsFallback = false,
            Action = action,
            QuickReplies = Array.Empty<string>()
        };
    }

    private static ChatbotResponseDto CreateFallbackResponse() => new()
    {
        IntentCode = "fallback",
        Category = ChatbotCategory.General,
        Answer = "Mình chưa hiểu rõ câu hỏi này. Bạn hãy chọn một chủ đề bên dưới hoặc liên hệ ME-ISM để được hỗ trợ.",
        Confidence = 0m,
        IsFallback = true,
        QuickReplies = DefaultQuickReplies
    };

    private static ChatbotCategory? ResolveRouteCategory(string? route)
    {
        if (string.IsNullOrWhiteSpace(route)) return null;

        var normalizedRoute = route.Trim().ToLowerInvariant();
        if (normalizedRoute.StartsWith("/customize", StringComparison.Ordinal)) return ChatbotCategory.Customization;
        if (normalizedRoute.StartsWith("/checkout", StringComparison.Ordinal)) return ChatbotCategory.Payment;
        if (normalizedRoute.StartsWith("/track", StringComparison.Ordinal)) return ChatbotCategory.OrderTracking;
        if (normalizedRoute.StartsWith("/shop", StringComparison.Ordinal) || normalizedRoute.StartsWith("/products", StringComparison.Ordinal)) return ChatbotCategory.Product;
        return null;
    }

    private static bool IsSafeInternalUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) &&
        url.StartsWith("/", StringComparison.Ordinal) &&
        !url.StartsWith("//", StringComparison.Ordinal) &&
        !url.Contains('\\');

    private static string NormalizeText(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;

        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var normalizedCharacter = character == 'đ' ? 'd' : character;
            if (char.IsLetterOrDigit(normalizedCharacter))
            {
                builder.Append(normalizedCharacter);
                lastWasSpace = false;
            }
            else if (!lastWasSpace && builder.Length > 0)
            {
                builder.Append(' ');
                lastWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    private static string HashClientKey(string clientKey)
    {
        var safeKey = string.IsNullOrWhiteSpace(clientKey) ? "anonymous" : clientKey.Trim();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(safeKey)));
    }

    private sealed record MatchResult(ChatbotKnowledgeEntry Entry, int Score);
}
