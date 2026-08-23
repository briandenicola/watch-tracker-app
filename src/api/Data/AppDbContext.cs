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
    public DbSet<WatchDisposition> WatchDispositions => Set<WatchDisposition>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OidcProviderSetting> OidcProviderSettings => Set<OidcProviderSetting>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<OidcLoginTicket> OidcLoginTickets => Set<OidcLoginTicket>();
    public DbSet<OidcState> OidcStates => Set<OidcState>();
    public DbSet<StyleSession> StyleSessions => Set<StyleSession>();
    public DbSet<StyleMessage> StyleMessages => Set<StyleMessage>();
    public DbSet<StyleRecommendation> StyleRecommendations => Set<StyleRecommendation>();
    public DbSet<AdvisorSession> AdvisorSessions => Set<AdvisorSession>();
    public DbSet<AdvisorMessage> AdvisorMessages => Set<AdvisorMessage>();
    public DbSet<AdvisorRecommendationFeedback> AdvisorRecommendationFeedback => Set<AdvisorRecommendationFeedback>();
    public DbSet<WatchShare> WatchShares => Set<WatchShare>();

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

            entity.Property(w => w.AcquisitionType)
                .HasConversion<string>();

            entity.Property(w => w.PurchasePrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(w => w.CurrentResaleValue)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(w => new { w.UserId, w.WishlistPriority })
                .IsUnique();

            entity.HasIndex(w => new { w.UserId, w.MarketplaceProvider, w.MarketplaceItemId })
                .IsUnique()
                .HasFilter("\"MarketplaceProvider\" IS NOT NULL AND \"MarketplaceItemId\" IS NOT NULL");

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

        modelBuilder.Entity<WatchShare>(entity =>
        {
            entity.HasOne(s => s.Watch)
                .WithMany()
                .HasForeignKey(s => s.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // The token is the lookup key for every public view of a watch.
            entity.HasIndex(s => s.Token).IsUnique();

            // One live link per watch, so revoking really does revoke.
            entity.HasIndex(s => s.WatchId).IsUnique();
        });

        modelBuilder.Entity<WatchDisposition>(entity =>
        {
            entity.HasKey(d => d.WatchId);

            entity.Property(d => d.Type)
                .HasConversion<string>();

            entity.Property(d => d.SalePrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(d => d.RefundAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(d => d.Watch)
                .WithOne(w => w.Disposition)
                .HasForeignKey<WatchDisposition>(d => d.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ReceivedWatch)
                .WithMany()
                .HasForeignKey(d => d.ReceivedWatchId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(d => d.ReceivedWatchId);
        });

        modelBuilder.Entity<StyleSession>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Watch)
                .WithMany()
                .HasForeignKey(s => s.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.UserId, s.WatchId, s.UpdatedAt });
        });

        modelBuilder.Entity<StyleMessage>(entity =>
        {
            entity.Property(m => m.Role)
                .HasConversion<string>();

            // The message keeps pointing at a recommendation the user later
            // forgets, so clearing a memory entry must not delete the transcript.
            entity.HasOne(m => m.Recommendation)
                .WithMany()
                .HasForeignKey(m => m.RecommendationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(m => new { m.SessionId, m.CreatedAt });
        });

        modelBuilder.Entity<StyleRecommendation>(entity =>
        {
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Watch)
                .WithMany()
                .HasForeignKey(r => r.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Memory survives the conversation that produced it.
            entity.HasOne(r => r.Session)
                .WithMany()
                .HasForeignKey(r => r.SessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(r => new { r.UserId, r.WatchId, r.CreatedAt });
        });

        modelBuilder.Entity<AdvisorSession>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.UserId, s.UpdatedAt });
        });

        modelBuilder.Entity<AdvisorMessage>(entity =>
        {
            entity.Property(m => m.Role)
                .HasConversion<string>();

            entity.HasIndex(m => new { m.SessionId, m.CreatedAt });
        });

        modelBuilder.Entity<AdvisorRecommendationFeedback>(entity =>
        {
            entity.Property(f => f.Provider).HasMaxLength(50);
            entity.Property(f => f.ProviderItemId).HasMaxLength(200);
            entity.Property(f => f.Title).HasMaxLength(500);
            entity.Property(f => f.Notes).HasMaxLength(500);
            entity.Property(f => f.Kind)
                .HasConversion<string>();

            entity.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Message)
                .WithMany(m => m.Feedback)
                .HasForeignKey(f => f.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(f => new { f.UserId, f.MessageId, f.Provider, f.ProviderItemId })
                .IsUnique();
            entity.HasIndex(f => new { f.UserId, f.UpdatedAt });
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
