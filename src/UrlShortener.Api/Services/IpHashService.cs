using System.Security.Cryptography;
using System.Text;

namespace UrlShortener.Api.Services;

public sealed class IpHashService
{
    private readonly byte[] _key;

    public IpHashService(IConfiguration configuration)
    {
        string key =
            configuration["Analytics:IpHashKey"]
            ?? throw new InvalidOperationException(
                "Analytics IP hash key is missing.");

        _key = Encoding.UTF8.GetBytes(key);
    }

    public string? Hash(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        byte[] input = Encoding.UTF8.GetBytes(ipAddress);

        using HMACSHA256 hmac = new(_key);

        byte[] hash = hmac.ComputeHash(input);

        return Convert.ToHexString(hash);
    }
}
