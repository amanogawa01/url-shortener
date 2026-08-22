using System.Text.Json;
using StackExchange.Redis;

namespace UrlShortener.Api.Services;

public sealed class UrlCacheService
{
    private readonly IDatabase _database;

    public UrlCacheService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<CachedUrl?> GetAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        RedisValue value = await _database.StringGetAsync(
            $"url:{code}");

        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<CachedUrl>(
            value.ToString());
    }

    public async Task SetAsync(
        string code,
        CachedUrl url,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        string value = JsonSerializer.Serialize(url);

        await _database.StringSetAsync(
            $"url:{code}",
            value,
            expiration);
    }

    public sealed record CachedUrl(
        string DestinationUrl,
        bool IsActive,
        DateTimeOffset? ExpiresAt);
}
