using StackExchange.Redis;

namespace UrlShortener.Api.Services;

public sealed class RateLimitService
{
    private readonly IDatabase _database;

    public RateLimitService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<RateLimitResult> CheckAsync(
        string key,
        int limit,
        TimeSpan window)
    {
        string redisKey = $"rate-limit:{key}";

        long count = await _database.StringIncrementAsync(redisKey);

        if (count == 1)
        {
            await _database.KeyExpireAsync(redisKey, window);
        }

        TimeSpan? ttl = await _database.KeyTimeToLiveAsync(redisKey);

        return new RateLimitResult(
            Allowed: count <= limit,
            Remaining: Math.Max(0, limit - (int)count),
            RetryAfter: ttl ?? window);
    }

    public sealed record RateLimitResult(
        bool Allowed,
        int Remaining,
        TimeSpan RetryAfter);
}
