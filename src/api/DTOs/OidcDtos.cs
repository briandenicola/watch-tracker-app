using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class OidcProviderPublicDto
{
    public required OidcProvider Provider { get; set; }
    public required string DisplayName { get; set; }
}

public class OidcProviderSettingsDto
{
    public required OidcProvider Provider { get; set; }
    public bool Enabled { get; set; }
    public required string DisplayName { get; set; }
    public required string Authority { get; set; }
    public required string ClientId { get; set; }
    public required string Scopes { get; set; }
    public bool HasClientSecret { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateOidcProviderSettingsDto
{
    public bool Enabled { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public required string DisplayName { get; set; }

    [Required, StringLength(2000), Url]
    public required string Authority { get; set; }

    [Required, StringLength(500)]
    public required string ClientId { get; set; }

    [Required, StringLength(500)]
    public required string Scopes { get; set; }
}

public class UpdateOidcProviderSecretDto
{
    [Required, StringLength(2000)]
    public required string ClientSecret { get; set; }
}

public class OidcProviderTestResultDto
{
    public bool Success { get; set; }
    public required string Message { get; set; }
}

public class OidcExchangeDto
{
    [Required]
    public required string Code { get; set; }
}

public class LinkedOidcProviderDto
{
    public required OidcProvider Provider { get; set; }
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public DateTime LinkedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
}
