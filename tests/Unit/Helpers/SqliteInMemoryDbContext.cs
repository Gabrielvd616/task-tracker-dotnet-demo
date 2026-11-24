using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskTrackerApi.Infrastructure.Data;

namespace TaskTrackerApi.UnitTests.Helpers;

/// <summary>
/// Builds a <see cref="TaskDbContext"/> backed by a shared open SQLite in-memory connection,
/// mirroring the SQLite-in-memory approach used by the PaymentApi unit tests. The connection is
/// kept open for the lifetime of this holder so the database survives between context operations.
/// </summary>
public sealed class SqliteInMemoryDbContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteInMemoryDbContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new TaskDbContext(options);
        Context.Database.EnsureCreated();
    }

    public TaskDbContext Context { get; }

    public TaskDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new TaskDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
