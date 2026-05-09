using Microsoft.EntityFrameworkCore;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Tests.Helpers;

public static class InMemoryDbHelper
{
    public static AppDbContext CreateContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? $"TestDb_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}
