namespace CaseShop.Web.Services.Security;

public interface IChatbotRateLimiter
{
    bool TryAcquire(string clientKey, out TimeSpan retryAfter);
}
