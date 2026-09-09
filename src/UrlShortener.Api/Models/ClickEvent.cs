namespace UrlShortener.Api.Models;

public sealed class ClickEvent
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string? Referrer { get; set; }

    public string? UserAgent { get; set; }

    public string? IpHash { get; set; }
}
