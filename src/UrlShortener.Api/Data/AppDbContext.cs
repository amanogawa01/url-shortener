using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.Property(x => x.Code)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.DestinationUrl)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();

            entity.Property(x => x.ClickCount)
                .HasDefaultValue(0);
        });
    }
}
