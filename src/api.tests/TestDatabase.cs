using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    private TestDatabase(SqliteConnection connection, AppDbContext context)
    {
        this.connection = connection;
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
        return new TestDatabase(connection, context);
    }

    public static User User(string username) => new()
    {
        Username = username,
        Email = $"{username}@example.test",
        PasswordHash = "not-used"
    };

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await connection.DisposeAsync();
    }
}
