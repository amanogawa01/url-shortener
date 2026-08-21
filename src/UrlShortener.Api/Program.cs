using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"));

});

builder.Services.AddOpenApi();
