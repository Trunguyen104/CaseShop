using System.Collections.Concurrent;

namespace CaseShop.Web.Services.Security;

public sealed class ChatbotRateLimiter : IChatbotRateLimiter
{
    private const int PermitLimit = 30;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, RequestWindow> _windows = new(StringComparer.Ordinal);
    private int _requestCounter;

    public bool TryAcquire(string clientKey, out TimeSpan retryAfter)
    {
        var safeKey = string.IsNullOrWhiteSpace(clientKey) ? "anonymous" : clientKey.Trim();
        var now = DateTime.UtcNow;
        if ((Interlocked.Increment(ref _requestCounter) & 1023) == 0)
        {
            RemoveExpiredWindows(now);
        }

        var requestWindow = _windows.GetOrAdd(safeKey, _ => new RequestWindow(now));

        lock (requestWindow.SyncRoot)
        {
            if (now - requestWindow.StartedAt >= Window)
            {
                requestWindow.StartedAt = now;
                requestWindow.Count = 0;
            }

            if (requestWindow.Count >= PermitLimit)
            {
                retryAfter = Window - (now - requestWindow.StartedAt);
                return false;
            }

            requestWindow.Count++;
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    private void RemoveExpiredWindows(DateTime now)
    {
        foreach (var pair in _windows)
        {
            if (now - pair.Value.StartedAt >= Window + Window)
            {
                _windows.TryRemove(pair.Key, out _);
            }
        }
    }

    private sealed class RequestWindow
    {
        public RequestWindow(DateTime startedAt)
        {
            StartedAt = startedAt;
        }

        public object SyncRoot { get; } = new();
        public DateTime StartedAt { get; set; }
        public int Count { get; set; }
    }
}
