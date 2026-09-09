using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UrlShortener.Api.Data;
using UrlShortener.Api.Endpoints;
using UrlShortener.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"));
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    string connectionString =
        builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException(
            "Redis connection string is missing.");

    return ConnectionMultiplexer.Connect(connectionString);
});
builder.Services.AddSingleton<UrlCacheService>();
builder.Services.AddSingleton<RateLimitService>();
builder.Services.AddSingleton<AnalyticsEventPublisher>();
builder.Services.AddSingleton<IpHashService>();
WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));

app.MapUrlEndpoints();

app.Run();
