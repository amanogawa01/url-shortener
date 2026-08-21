namespace UrlShortener.Api.Models;

public class ShortUrl
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public required string DestinationUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public long ClickCount { get; set; }
}
