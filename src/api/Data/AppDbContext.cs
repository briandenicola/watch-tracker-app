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
        });
    }
}
