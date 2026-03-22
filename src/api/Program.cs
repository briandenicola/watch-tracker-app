using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WatchTracker.Api.Authentication;
using WatchTracker.Api.Data;
using WatchTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Dynamic configuration source for runtime log-level changes
var dynamicConfigSource = new DynamicConfigurationSource();
((IConfigurationBuilder)builder.Configuration).Add(dynamicConfigSource);
builder.Services.AddSingleton(dynamicConfigSource.Provider);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=watchtracker.db"));

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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
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

// Configure forwarded headers for reverse-proxy deployments (nginx, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

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

builder.Services.AddHttpClient();
builder.Services.AddScoped<IWatchService, WatchService>();
builder.Services.AddScoped<IWatchImageService, WatchImageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddHttpClient<IWatchAnalysisService, WatchAnalysisService>();

var app = builder.Build();

// Must be first middleware for correct scheme/host resolution behind proxies
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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
