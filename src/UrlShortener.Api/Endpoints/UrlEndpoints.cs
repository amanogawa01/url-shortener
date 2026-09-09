using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Endpoints;

public static class UrlEndpoints
{
    public static void MapUrlEndpoints(this WebApplication app)
    {
        app.MapPost("/api/urls", async (
            CreateUrlRequest request,
            HttpContext httpContext,
            AppDbContext db,
            RateLimitService rateLimiter,
            CancellationToken cancellationToken) =>
        {
            string clientIp =
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            RateLimitService.RateLimitResult rateLimit =
                await rateLimiter.CheckAsync(
                    $"create:{clientIp}",
                    limit: 10,
                    window: TimeSpan.FromMinutes(1));

            httpContext.Response.Headers["X-RateLimit-Limit"] = "10";
            httpContext.Response.Headers["X-RateLimit-Remaining"] =
                rateLimit.Remaining.ToString();

            if (!rateLimit.Allowed)
            {
                httpContext.Response.Headers["Retry-After"] =
                    Math.Ceiling(rateLimit.RetryAfter.TotalSeconds).ToString();

                return Results.StatusCode(
                    StatusCodes.Status429TooManyRequests);
            }

            if (!Uri.TryCreate(
                    request.Url,
                    UriKind.Absolute,
                    out Uri? destination))
            {
                return Results.BadRequest(new
                {
                    error = "Invalid URL."
                });
            }

            if (destination.Scheme is not ("http" or "https"))
            {
                return Results.BadRequest(new
                {
                    error = "Only HTTP and HTTPS URLs are supported."
                });
            }

            string code = GenerateCode();

            var shortUrl = new ShortUrl
            {
                Id = Guid.NewGuid(),
                Code = code,
                DestinationUrl = destination.ToString(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.ShortUrls.Add(shortUrl);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/{code}",
                new CreateUrlResponse(
                    code,
                    destination.ToString()));
        });

        app.MapGet("/api/urls/{code}", async (
            string code,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            ShortUrl? url = await db.ShortUrls
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Code == code,
                    cancellationToken);

            if (url is null)
            {
                return Results.NotFound(new
                {
                    error = "Short URL not found."
                });
            }

            return Results.Ok(url);
        });

        app.MapGet("/{code}", async (
            string code,
            HttpContext httpContext,
            AppDbContext db,
            UrlCacheService cache,
            AnalyticsEventPublisher analytics,
            IpHashService ipHasher,
            CancellationToken cancellationToken) =>
        {
            UrlCacheService.CachedUrl? cachedUrl =
                await cache.GetAsync(
                    code,
                    cancellationToken);

            if (cachedUrl is not null)
            {
                if (!cachedUrl.IsActive)
                {
                    return Results.NotFound(new
                    {
                        error = "Short URL is inactive."
                    });
                }

                if (cachedUrl.ExpiresAt is not null &&
                    cachedUrl.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    return Results.NotFound(new
                    {
                        error = "Short URL has expired."
                    });
                }

                await analytics.PublishClickAsync(
                    code,
                    httpContext.Request.Headers.Referer.ToString(),
                    httpContext.Request.Headers.UserAgent.ToString(),
                    ipHasher.Hash(
                        httpContext.Connection.RemoteIpAddress?.ToString()));

                return Results.Redirect(
                    cachedUrl.DestinationUrl,
                    permanent: false);
            }

            ShortUrl? url = await db.ShortUrls
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Code == code,
                    cancellationToken);

            if (url is null)
            {
                return Results.NotFound(new
                {
                    error = "Short URL not found."
                });
            }

            if (!url.IsActive)
            {
                return Results.NotFound(new
                {
                    error = "Short URL is inactive."
                });
            }

            if (url.ExpiresAt is not null &&
                url.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Results.NotFound(new
                {
                    error = "Short URL has expired."
                });
            }

            await cache.SetAsync(
                code,
                new UrlCacheService.CachedUrl(
                    url.DestinationUrl,
                    url.IsActive,
                    url.ExpiresAt),
                TimeSpan.FromHours(24),
                cancellationToken);

            await analytics.PublishClickAsync(
                code,
                httpContext.Request.Headers.Referer.ToString(),
                httpContext.Request.Headers.UserAgent.ToString(),
                ipHasher.Hash(
                    httpContext.Connection.RemoteIpAddress?.ToString()));

            return Results.Redirect(
                url.DestinationUrl,
                permanent: false);
        });
    }

    private static string GenerateCode()
    {
        const string characters =
            "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        Span<char> code = stackalloc char[7];

        for (int i = 0; i < code.Length; i++)
        {
            code[i] = characters[Random.Shared.Next(characters.Length)];
        }

        return new string(code);
    }

    public sealed record CreateUrlRequest(string Url);

    public sealed record CreateUrlResponse(
        string Code,
        string DestinationUrl);
}
