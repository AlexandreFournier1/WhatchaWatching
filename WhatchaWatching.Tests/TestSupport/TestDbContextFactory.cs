using Microsoft.EntityFrameworkCore;
using WhatchaWatching.Infrastructure.Data;

namespace WhatchaWatching.Tests.TestSupport;

/// <summary>Every context it creates shares one in-memory database, like the real factory shares one MySQL database.</summary>
public sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    public ApplicationDbContext CreateDbContext() => new(_options);
}
