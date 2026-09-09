using StackExchange.Redis;

namespace UrlShortener.Api.Services;

public sealed class AnalyticsEventPublisher
{
    private const string StreamName = "analytics:clicks";

    private readonly IDatabase _database;

    public AnalyticsEventPublisher(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task PublishClickAsync(
        string code,
        string? referrer,
        string? userAgent,
        string? ipHash)
    {
        NameValueEntry[] values =
        [
            new("code", code),
            new(
                "timestamp",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            new("referrer", referrer ?? string.Empty),
            new("userAgent", userAgent ?? string.Empty),
            new("ipHash", ipHash ?? string.Empty)
        ];

        await _database.StreamAddAsync(
            StreamName,
            values);
    }
}
