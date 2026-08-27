using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WatchTracker.Api.Authentication;
using WatchTracker.Api.Configuration;
using WatchTracker.Api.Data;
using WatchTracker.Api.Diagnostics;
using WatchTracker.Api.Services;
using WatchTracker.Api.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Dynamic configuration source for runtime log-level changes
var dynamicConfigSource = new DynamicConfigurationSource();
((IConfigurationBuilder)builder.Configuration).Add(dynamicConfigSource);
builder.Services.AddSingleton(dynamicConfigSource.Provider);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=watchtracker.db"));

var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "data", "keys");
Directory.CreateDirectory(dataProtectionKeyPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));

// Validate JWT configuration at startup
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key must be configured and at least 32 bytes long. " +
        "Set it via appsettings.json, environment variable (Jwt__Key), or user secrets.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "JwtOrApiKey";
        options.DefaultChallengeScheme = "JwtOrApiKey";
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, null)
    .AddPolicyScheme("JwtOrApiKey", "JWT or API Key", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            if (context.Request.Headers.ContainsKey(ApiKeyAuthenticationHandler.HeaderName))
                return ApiKeyAuthenticationHandler.SchemeName;
            return JwtBearerDefaults.AuthenticationScheme;
        };
    });

builder.Services.AddAuthorization();

// Only explicitly configured proxy networks may supply client address or scheme.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    TrustedProxyNetworks.Configure(
        options,
        builder.Configuration["ForwardedHeaders:TrustedNetworks"]));

builder.Services.AddCors(options =>
{
    var originsValue = builder.Configuration.GetValue<string>("AllowedOrigins") ?? "http://localhost:5173";
    options.AddDefaultPolicy(policy =>
    {
        if (originsValue == "*")
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(originsValue.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// Rate limiting for auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting");
        logger.LogWarning(
            "Request rejected by rate limiting for endpoint {Endpoint}.",
            context.HttpContext.GetEndpoint()?.DisplayName ?? "unknown");
        return ValueTask.CompletedTask;
    };

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Shared links are the app's only unauthenticated surface, so the public
    // read is capped per IP — enough for a page and its refreshes, not enough
    // to walk the token space.
    options.AddPolicy("public-share", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("style-agent", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("resale-refresh", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("watch-recommendation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("wishlist-extraction", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // The most expensive call in the app: a whole-collection prompt plus a
    // model round trip, so the limit is deliberately the tightest here.
    options.AddPolicy("collection-review", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("collection-advisor", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<IWatchService, WatchService>();
builder.Services.AddScoped<ICollectionProfileService, CollectionProfileService>();
builder.Services.AddScoped<IRecommendationWishlistService, RecommendationWishlistService>();
builder.Services.AddScoped<ICollectionAdvisorService, CollectionAdvisorService>();
builder.Services.AddHttpClient<ICollectionReviewService, CollectionReviewService>()
    .ConfigureHttpClient(c => c.Timeout =
        CollectionReviewService.MaxExecutionTime + TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient<ICollectionReviewCandidateService, CollectionReviewCandidateService>()
    .ConfigureHttpClient(c => c.Timeout =
        CollectionReviewCandidateService.MaxExecutionTime + TimeSpan.FromSeconds(30));
builder.Services.AddScoped<IAdvisorToolService, AdvisorToolService>();
builder.Services.AddScoped<IWatchImageService, WatchImageService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IWatchShareService, WatchShareService>();
builder.Services.AddScoped<IWishlistShareService, WishlistShareService>();
builder.Services.AddSingleton<IBackgroundRemovalService, BackgroundRemovalService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOidcService, OidcService>();
builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddHttpClient<IWatchAnalysisService, WatchAnalysisService>();
builder.Services.AddHttpClient<IWishlistExtractionService, WishlistExtractionService>();
// Fetches whatever page a watch links to, so the analysis can read a spec sheet
// instead of guessing. Its handler refuses to connect to anything but a public
// address — see ProductPageReader for why that lives on the handler.
builder.Services.AddHttpClient<IProductPageReader, ProductPageReader>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => ProductPageReader.CreateHandler());
builder.Services.AddHttpClient("RemoteImage")
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(15))
    .ConfigurePrimaryHttpMessageHandler(() => ProductPageReader.CreateHandler());
// These clients talk to Ollama and retain its default timeout because AI
// generation can legitimately take longer than a typical API call.
builder.Services.AddHttpClient<IStyleAgentService, StyleAgentService>();
builder.Services.AddHttpClient<IWatchRecommendationService, WatchRecommendationService>();
builder.Services.AddHttpClient<IAdvisorReplyGenerator, AdvisorReplyGenerator>();
builder.Services.AddHttpClient<IWebSearchClient, BraveSearchClient>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHttpClient<IWebSearchClient, SearXngSearchClient>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHttpClient<ISearXngTestClient, SearXngSearchClient>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(20));
// No explicit timeout override here — this client's HttpClient is used for the Ollama call,
// which (like WatchAnalysisService's) can legitimately take longer than a typical HTTP API call.
builder.Services.AddHttpClient<IResaleValueEstimator, WebSearchOllamaResaleValueEstimator>();
builder.Services.AddHttpClient("EbayToken")
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddSingleton<IEbayTokenProvider, EbayTokenProvider>();
builder.Services.AddHttpClient<IEbayBrowseClient, EbayBrowseClient>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddScoped<IMarketplaceSearchClient>(
    services => services.GetRequiredService<IEbayBrowseClient>());
builder.Services.AddScoped<IResaleValueEstimator, EbayResaleValueEstimator>();
builder.Services.AddScoped<IResaleValueRefreshService, ResaleValueRefreshService>();
builder.Services.AddHostedService<ResaleValueRefreshBackgroundService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();

var app = builder.Build();

// Must be first middleware for correct scheme/host resolution behind proxies
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();

var uploadsDir = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsDir);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

// Serve the React SPA from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

// A share link answers with JSON when asked, so the same URL works for a person
// and for a script: /s/<token>?format=json. Anything else falls through to the
// SPA, which renders the page.
app.MapGet("/s/{token}", async (
        string token,
        [FromQuery] string? format,
        IWatchShareService shares,
        IWebHostEnvironment environment,
        CancellationToken ct) =>
    {
        if (!string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            var webRoot = environment.WebRootPath;
            if (string.IsNullOrEmpty(webRoot)) return Results.NotFound();

            var indexPath = Path.Combine(webRoot, "index.html");
            return File.Exists(indexPath)
                ? Results.File(indexPath, "text/html")
                : Results.NotFound();
        }

        var watch = await shares.ViewAsync(token, ct);
        return watch is null
            ? Results.NotFound(new { error = "This share link is not available." })
            : Results.Ok(watch);
    })
    .RequireRateLimiting("public-share")
    .AllowAnonymous();

// The wish list link answers with JSON on the same terms as a watch link.
app.MapGet("/w/{token}", async (
        string token,
        [FromQuery] string? format,
        IWishlistShareService shares,
        IWebHostEnvironment environment,
        CancellationToken ct) =>
    {
        if (!string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            var webRoot = environment.WebRootPath;
            if (string.IsNullOrEmpty(webRoot)) return Results.NotFound();

            var indexPath = Path.Combine(webRoot, "index.html");
            return File.Exists(indexPath)
                ? Results.File(indexPath, "text/html")
                : Results.NotFound();
        }

        var wishlist = await shares.ViewAsync(token, ct);
        return wishlist is null
            ? Results.NotFound(new { error = "This share link is not available." })
            : Results.Ok(wishlist);
    })
    .RequireRateLimiting("public-share")
    .AllowAnonymous();

// SPA fallback — serve index.html for client-side routes
app.MapFallbackToFile("index.html");

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Seed runtime log level from database setting
    var settingsService = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();
    var storedLogLevel = await settingsService.GetAsync(AppSettingsService.Keys.LogLevel, "Information");
    var dynConfig = scope.ServiceProvider.GetRequiredService<DynamicConfigurationProvider>();
    dynConfig.Set("Logging:LogLevel:Default", storedLogLevel);
}

app.Run();
