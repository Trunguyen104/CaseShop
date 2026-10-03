using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;
using CaseShop.Web.Repositories;
using CaseShop.Web.Services;
using CaseShop.Web.Services.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

await MatchesVietnameseWithoutDiacriticsAsync();
await ReturnsFallbackForUnknownQuestionAsync();
await HighPriorityIntentDoesNotCaptureUnknownQuestionAsync();
await MatchesPreparedQuickReplyAsync();
await MatchesNaturalOrderTrackingQuestionAsync();
await RejectsInvalidQuestionAsync();
await RejectsRateLimitedClientAsync();
RateLimiterBlocksTheThirtyFirstRequest();
await OmitsUnsafeExternalActionAsync();
await CachesKnowledgeEntriesAsync();
RepositoryUsesBoundedNoTrackingQuery();
Console.WriteLine("PASS: chatbot matching, fallback, validation, rate limiting, safe actions, caching and repository query safeguards.");

static async Task MatchesVietnameseWithoutDiacriticsAsync()
{
    // Arrange
    var service = CreateService([
        Entry("track_order", "Làm thế nào để theo dõi đơn hàng?", "theo dõi đơn;trạng thái đơn", "/track")
    ]);

    // Act
    var response = await service.GetResponseAsync(new ChatbotRequestDto { Message = "Toi muon theo doi don" }, "client-a");

    // Assert
    Assert(response.IntentCode == "track_order", "Vietnamese text without diacritics did not match the prepared intent.");
    Assert(response.Action?.Url == "/track", "Safe internal action was not returned.");
    Assert(!response.IsFallback, "A valid intent unexpectedly returned fallback.");
}

static async Task ReturnsFallbackForUnknownQuestionAsync()
{
    // Arrange
    var service = CreateService([Entry("payment", "Thanh toán như thế nào?", "thanh toán;payos", "/checkout")]);

    // Act
    var response = await service.GetResponseAsync(new ChatbotRequestDto { Message = "Một câu hoàn toàn không liên quan" }, "client-b");

    // Assert
    Assert(response.IsFallback, "Unknown question did not return fallback.");
    Assert(response.QuickReplies.Count >= 5, "Fallback did not provide navigation choices.");
}

static async Task HighPriorityIntentDoesNotCaptureUnknownQuestionAsync()
{
    var entry = Entry("customize_start", "Làm thế nào để tự thiết kế ốp lưng?", "thiết kế ốp lưng;thiết kế", "/customize");
    entry.Priority = 100;
    entry.Category = ChatbotCategory.Customization;
    var service = CreateService([entry]);

    var response = await service.GetResponseAsync(
        new ChatbotRequestDto { Message = "Thời tiết hôm nay thế nào?", CurrentRoute = "/customize" },
        "client-unknown");

    Assert(response.IsFallback, "A high-priority intent captured an unrelated question.");
}

static async Task MatchesPreparedQuickReplyAsync()
{
    var entry = Entry("customize_start", "Làm thế nào để tự thiết kế ốp lưng?", "thiết kế ốp lưng;thiết kế", "/customize");
    entry.Priority = 100;
    entry.Category = ChatbotCategory.Customization;
    var service = CreateService([entry]);

    var response = await service.GetResponseAsync(
        new ChatbotRequestDto { Message = "Thiết kế ốp lưng" },
        "client-quick-reply");

    Assert(response.IntentCode == "customize_start", "The customization quick reply did not match its prepared intent.");
}

static async Task MatchesNaturalOrderTrackingQuestionAsync()
{
    var entry = Entry("track_order", "Làm thế nào để theo dõi đơn hàng?", "theo dõi đơn;đơn hàng của tôi;đơn hàng tới đâu", "/track");
    entry.Priority = 100;
    entry.Category = ChatbotCategory.OrderTracking;
    var service = CreateService([entry]);

    var response = await service.GetResponseAsync(
        new ChatbotRequestDto { Message = "Đơn hàng của tôi tới đâu rồi?" },
        "client-natural-question");

    Assert(response.IntentCode == "track_order", "A natural order-tracking question did not match the tracking intent.");
}

static async Task RejectsInvalidQuestionAsync()
{
    // Arrange
    var service = CreateService([]);

    // Act & Assert
    await ExpectAsync<ArgumentException>(
        () => service.GetResponseAsync(new ChatbotRequestDto { Message = "   " }, "client-c"),
        "Blank question was accepted.");

    await ExpectAsync<ArgumentException>(
        () => service.GetResponseAsync(new ChatbotRequestDto { Message = new string('a', 501) }, "client-c"),
        "Question longer than 500 characters was accepted.");
}

static async Task RejectsRateLimitedClientAsync()
{
    // Arrange
    var service = CreateService([], new RejectingRateLimiter());

    // Act & Assert
    await ExpectAsync<InvalidOperationException>(
        () => service.GetResponseAsync(new ChatbotRequestDto { Message = "Giá sản phẩm" }, "client-d"),
        "Rate-limited question was accepted.");
}

static void RateLimiterBlocksTheThirtyFirstRequest()
{
    // Arrange
    var limiter = new ChatbotRateLimiter();

    // Act & Assert
    for (var index = 0; index < 30; index++)
    {
        Assert(limiter.TryAcquire("rate-limit-client", out _), $"Request {index + 1} was rejected before the configured limit.");
    }

    Assert(!limiter.TryAcquire("rate-limit-client", out var retryAfter), "The thirty-first request was not rate limited.");
    Assert(retryAfter > TimeSpan.Zero, "Rate limiter did not return a retry duration.");
}

static async Task OmitsUnsafeExternalActionAsync()
{
    // Arrange
    var service = CreateService([
        Entry("unsafe", "Tôi cần hỗ trợ", "hỗ trợ", "https://malicious.example")
    ]);

    // Act
    var response = await service.GetResponseAsync(new ChatbotRequestDto { Message = "Tôi cần hỗ trợ" }, "client-e");

    // Assert
    Assert(response.IntentCode == "unsafe", "Prepared answer was not selected.");
    Assert(response.Action is null, "External action URL was exposed to the client.");
}

static async Task CachesKnowledgeEntriesAsync()
{
    // Arrange
    var repository = new FakeRepository([Entry("price", "Giá bao nhiêu?", "giá", "/shop")]);
    var service = CreateService(repository: repository);

    // Act
    await service.GetResponseAsync(new ChatbotRequestDto { Message = "Giá bao nhiêu?" }, "client-f");
    await service.GetResponseAsync(new ChatbotRequestDto { Message = "Giá" }, "client-f");

    // Assert
    Assert(repository.CallCount == 1, "Active chatbot knowledge was not cached.");
}

static void RepositoryUsesBoundedNoTrackingQuery()
{
    // Arrange
    var sourcePath = Path.Combine("CaseShop.Web", "Repositories", "ChatbotKnowledgeRepository.cs");
    var source = File.ReadAllText(sourcePath);

    // Act & Assert
    Assert(source.Contains(".AsNoTracking()", StringComparison.Ordinal), "Repository read query is missing AsNoTracking.");
    Assert(source.Contains(".Take(500)", StringComparison.Ordinal), "Repository read query is not bounded.");
}

static ChatbotService CreateService(
    IReadOnlyList<ChatbotKnowledgeEntry>? entries = null,
    IChatbotRateLimiter? rateLimiter = null,
    FakeRepository? repository = null)
{
    var resolvedRepository = repository ?? new FakeRepository(entries ?? []);
    return new ChatbotService(
        resolvedRepository,
        rateLimiter ?? new AllowingRateLimiter(),
        new MemoryCache(new MemoryCacheOptions()),
        NullLogger<ChatbotService>.Instance);
}

static ChatbotKnowledgeEntry Entry(string intent, string question, string keywords, string? actionUrl) => new()
{
    Id = Guid.NewGuid(),
    IntentCode = intent,
    Category = ChatbotCategory.General,
    Question = question,
    Answer = $"Prepared answer for {intent}",
    Keywords = keywords,
    ActionType = actionUrl is null ? ChatbotActionType.None : ChatbotActionType.Link,
    ActionLabel = actionUrl is null ? null : "Mở trang",
    ActionUrl = actionUrl,
    Priority = 10,
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};

static async Task ExpectAsync<TException>(Func<Task> action, string failureMessage) where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(failureMessage);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class FakeRepository : IChatbotKnowledgeRepository
{
    private readonly IReadOnlyList<ChatbotKnowledgeEntry> _entries;

    public FakeRepository(IReadOnlyList<ChatbotKnowledgeEntry> entries)
    {
        _entries = entries;
    }

    public int CallCount { get; private set; }

    public Task<IReadOnlyList<ChatbotKnowledgeEntry>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(_entries);
    }
}

sealed class AllowingRateLimiter : IChatbotRateLimiter
{
    public bool TryAcquire(string clientKey, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        return true;
    }
}

sealed class RejectingRateLimiter : IChatbotRateLimiter
{
    public bool TryAcquire(string clientKey, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.FromSeconds(30);
        return false;
    }
}
