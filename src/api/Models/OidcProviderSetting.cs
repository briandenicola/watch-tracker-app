namespace WatchTracker.Api.Models;

public class OidcProviderSetting
{
    public int Id { get; set; }
    public OidcProvider Provider { get; set; }
    public bool Enabled { get; set; }
    public required string DisplayName { get; set; }
    public required string Authority { get; set; }
    public required string ClientId { get; set; }
    public string? ClientSecretProtected { get; set; }
    public required string Scopes { get; set; } = "openid profile email";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
