using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();

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
        modelBuilder.Entity<ClickEvent>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.Timestamp)
                .IsRequired();

            entity.Property(x => x.Referrer)
                .HasMaxLength(2048);

            entity.Property(x => x.UserAgent)
                .HasMaxLength(1024);

            entity.Property(x => x.IpHash)
                .HasMaxLength(128);
        });
    }
}
