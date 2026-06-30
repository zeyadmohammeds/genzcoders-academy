using System.Collections.Concurrent;

namespace GenZCoders.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, RateLimitEntry> _clients = new();
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private DateTime _lastCleanup = DateTime.UtcNow;

    public RateLimitingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method;
        var ip = GetClientIp(context);
        var key = $"{ip}:{method}:{path}";

        var limits = GetRateLimit(method, path);
        if (limits is null)
        {
            await _next(context);
            return;
        }

        var (maxRequests, windowMs) = limits.Value;
        var entry = _clients.GetOrAdd(key, _ => new RateLimitEntry());
        lock (entry)
        {
            var now = DateTime.UtcNow;
            entry.Timestamps.RemoveAll(t => now - t > TimeSpan.FromMilliseconds(windowMs));

            if (entry.Timestamps.Count >= maxRequests)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = (windowMs / 1000).ToString();
                context.Response.Headers["Content-Type"] = "application/json";
                var retryAfter = (entry.Timestamps[0] - now).TotalMilliseconds;
                context.Response.WriteAsync($"{{\"error\":\"Rate limit exceeded. Try again in {(int)retryAfter / 1000}s.\"}}");
                return;
            }

            entry.Timestamps.Add(now);
        }

        PeriodicCleanup();

        await _next(context);
    }

    private static (int maxRequests, int windowMs)? GetRateLimit(string method, string path)
    {
        if (method == "POST" && path.Contains("/api/auth/login")) return (5, 15 * 60 * 1000);
        if (method == "POST" && path.Contains("/api/auth/register")) return (3, 60 * 60 * 1000);
        if (method == "POST" && path.Contains("/api/auth/forgot-password")) return (3, 60 * 60 * 1000);
        if (method == "POST" && path.Contains("/api/enrollments")) return (10, 60 * 60 * 1000);
        if (method == "GET" && path.Contains("/api/courses") && !path.Contains("/api/admin")) return (100, 60 * 1000);
        if (path.Contains("/api/payments/")) return (10, 5 * 60 * 1000);
        if (path.Contains("/api/schools/apply")) return (5, 60 * 60 * 1000);
        if (path.Contains("/api/") && !path.Contains("/api/auth/me") && !path.Contains("/health")) return (60, 60 * 1000);
        return null;
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded)) return forwarded.Split(',')[0].Trim();
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private void PeriodicCleanup()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup > CleanupInterval)
        {
            _lastCleanup = now;
            foreach (var key in _clients.Keys)
            {
                if (_clients.TryGetValue(key, out var entry))
                {
                    lock (entry)
                    {
                        entry.Timestamps.RemoveAll(t => now - t > CleanupInterval);
                        if (entry.Timestamps.Count == 0) _clients.TryRemove(key, out _);
                    }
                }
            }
        }
    }

    private class RateLimitEntry
    {
        public List<DateTime> Timestamps { get; set; } = new();
    }
}
