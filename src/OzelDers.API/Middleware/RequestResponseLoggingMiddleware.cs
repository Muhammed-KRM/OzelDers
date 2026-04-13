using System.Security.Claims;
using System.Text;
using OzelDers.Business.Interfaces;

namespace OzelDers.API.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] SkipPaths =
        ["/swagger", "/favicon", "/uploads", "/_blazor", "/health"];

    public RequestResponseLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogService logService)
    {
        var path = context.Request.Path.Value ?? "";

        if (SkipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Request body'yi buffer'a al (stream tek okunabilir)
        context.Request.EnableBuffering();
        var requestBody = await ReadBodyAsync(context.Request.Body);
        context.Request.Body.Position = 0;

        // Response body'yi yakala
        var originalResponseBody = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        await _next(context);

        stopwatch.Stop();

        responseBuffer.Position = 0;
        var responseBody = await new StreamReader(responseBuffer).ReadToEndAsync();
        responseBuffer.Position = 0;
        await responseBuffer.CopyToAsync(originalResponseBody);
        context.Response.Body = originalResponseBody;

        // Kullanıcı bilgisini JWT'den al
        Guid? userId = null;
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var parsedId)) userId = parsedId;
        var userEmail = context.User.FindFirstValue(ClaimTypes.Email);

        // Fire-and-forget: isteği yavaşlatmaz
        _ = logService.LogEndpointAsync(new EndpointLogEntry
        {
            TraceId      = context.TraceIdentifier,
            Method       = context.Request.Method,
            Path         = path,
            Query        = context.Request.QueryString.Value,
            RequestBody  = requestBody,
            ResponseBody = responseBody.Length > 5000
                ? responseBody[..5000] + "...[truncated]"
                : responseBody,
            StatusCode   = context.Response.StatusCode,
            UserId       = userId,
            UserEmail    = userEmail,
            IpAddress    = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent    = context.Request.Headers.UserAgent.ToString(),
            DurationMs   = (int)stopwatch.ElapsedMilliseconds
        });
    }

    private static async Task<string?> ReadBodyAsync(Stream body)
    {
        if (!body.CanRead || body.Length == 0) return null;
        using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
