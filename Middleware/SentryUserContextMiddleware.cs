using Sentry;
using System.Security.Claims;

namespace GenZCoders.Middleware
{
    public class SentryUserContextMiddleware
    {
        private readonly RequestDelegate _next;

        public SentryUserContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.User = new SentryUser
                    {
                        Id = context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        Email = context.User.FindFirstValue(ClaimTypes.Email),
                        Username = context.User.Identity.Name,
                        IpAddress = context.Connection.RemoteIpAddress?.ToString()
                    };
                });
            }

            await _next(context);
        }
    }

    public static class SentryUserContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseSentryUserContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SentryUserContextMiddleware>();
        }
    }
}
