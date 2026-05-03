using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Watch> Watches => Set<Watch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<WatchImage> WatchImages => Set<WatchImage>();
    public DbSet<WearLog> WearLogs => Set<WearLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.Role)
                .HasConversion<string>();
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(a => a.Key);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => a.KeyHash).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rt => rt.TokenHash).IsUnique();
        });

        modelBuilder.Entity<Watch>(entity =>
        {
            entity.HasOne(w => w.User)
                .WithMany(u => u.Watches)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(w => w.MovementType)
                .HasConversion<string>();

            entity.Property(w => w.PurchasePrice)
                .HasColumnType("decimal(18,2)");

            entity.HasMany(w => w.Images)
                .WithOne(i => i.Watch)
                .HasForeignKey(i => i.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(w => w.WearLogs)
                .WithOne(wl => wl.Watch)
                .HasForeignKey(wl => wl.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(w => w.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }
}
