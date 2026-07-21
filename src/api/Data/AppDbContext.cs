using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Watch> Watches => Set<Watch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<WatchImage> WatchImages => Set<WatchImage>();
    public DbSet<WearLog> WearLogs => Set<WearLog>();
    public DbSet<ResaleValueEntry> ResaleValueEntries => Set<ResaleValueEntry>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OidcProviderSetting> OidcProviderSettings => Set<OidcProviderSetting>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<OidcLoginTicket> OidcLoginTickets => Set<OidcLoginTicket>();
    public DbSet<OidcState> OidcStates => Set<OidcState>();

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

        modelBuilder.Entity<OidcProviderSetting>(entity =>
        {
            entity.Property(s => s.Provider)
                .HasConversion<string>();

            entity.HasIndex(s => s.Provider).IsUnique();
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.Property(l => l.Provider)
                .HasConversion<string>();

            entity.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => new { l.Provider, l.Issuer, l.ProviderSubject }).IsUnique();
            entity.HasIndex(l => new { l.UserId, l.Provider }).IsUnique();
        });

        modelBuilder.Entity<OidcLoginTicket>(entity =>
        {
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => t.CodeHash).IsUnique();
        });

        modelBuilder.Entity<OidcState>(entity =>
        {
            entity.Property(s => s.Provider)
                .HasConversion<string>();

            entity.HasOne(s => s.LinkUser)
                .WithMany()
                .HasForeignKey(s => s.LinkUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => s.StateHash).IsUnique();
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

            entity.Property(w => w.CurrentResaleValue)
                .HasColumnType("decimal(18,2)");

            entity.HasMany(w => w.Images)
                .WithOne(i => i.Watch)
                .HasForeignKey(i => i.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(w => w.WearLogs)
                .WithOne(wl => wl.Watch)
                .HasForeignKey(wl => wl.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(w => w.ResaleValueEntries)
                .WithOne(r => r.Watch)
                .HasForeignKey(r => r.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(w => w.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<ResaleValueEntry>(entity =>
        {
            entity.Property(r => r.Source)
                .HasConversion<string>();

            entity.Property(r => r.Value)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(r => new { r.WatchId, r.RecordedAt });
        });
    }
}
