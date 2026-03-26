using AuntiesRecipe.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Tests;

internal sealed class SqliteDbFactory : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public SqliteDbFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    public AppDbContext CreateDbContext() => new(_options);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new AppDbContext(_options));

    public void Dispose()
    {
        _connection.Dispose();
    }
}
