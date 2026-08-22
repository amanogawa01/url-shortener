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
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!Uri.TryCreate(
                    request.Url,
                    UriKind.Absolute,
                    out var destination))
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

            var code = GenerateCode();

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
            var url = await db.ShortUrls
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
            AppDbContext db,
            UrlCacheService cache,
            CancellationToken cancellationToken) =>
        {
            var cachedUrl = await cache.GetAsync(
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

                return Results.Redirect(
                    cachedUrl.DestinationUrl,
                    permanent: false);
            }

            var url = await db.ShortUrls
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

        for (var i = 0; i < code.Length; i++)
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
