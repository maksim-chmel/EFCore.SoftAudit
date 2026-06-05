using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit.Tests;

public sealed class SoftDeleteQueryableExtensionsTests
{
    private static TestDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task WithDeleted_ShouldReturnAllEntities_IncludingDeleted()
    {
        await using var db = CreateDb();
        var active = new TestOrder { Name = "Active" };
        var deleted = new TestOrder { Name = "Deleted" };
        db.Orders.AddRange(active, deleted);
        await db.SaveChangesAsync();
        db.Orders.Remove(deleted);
        await db.SaveChangesAsync();

        var results = await db.Orders.WithDeleted().ToListAsync();
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task WithDeleted_ShouldReturnOnlyActive_WhenNoneDeleted()
    {
        await using var db = CreateDb();
        db.Orders.Add(new TestOrder { Name = "Active" });
        await db.SaveChangesAsync();

        var results = await db.Orders.WithDeleted().ToListAsync();
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task OnlyDeleted_ShouldReturnOnlyDeletedEntities()
    {
        await using var db = CreateDb();
        var active = new TestOrder { Name = "Active" };
        var deleted = new TestOrder { Name = "Deleted" };
        db.Orders.AddRange(active, deleted);
        await db.SaveChangesAsync();
        db.Orders.Remove(deleted);
        await db.SaveChangesAsync();

        var results = await db.Orders.OnlyDeleted().ToListAsync();
        results.Should().ContainSingle(o => o.Name == "Deleted");
    }

    [Fact]
    public async Task OnlyDeleted_ShouldReturnEmpty_WhenNoneDeleted()
    {
        await using var db = CreateDb();
        db.Orders.Add(new TestOrder { Name = "Active" });
        await db.SaveChangesAsync();

        var results = await db.Orders.OnlyDeleted().ToListAsync();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task OnlyDeleted_ShouldReturnAllDeletedEntities_WhenMultipleDeleted()
    {
        await using var db = CreateDb();
        var d1 = new TestOrder { Name = "Deleted1" };
        var d2 = new TestOrder { Name = "Deleted2" };
        var active = new TestOrder { Name = "Active" };
        db.Orders.AddRange(d1, d2, active);
        await db.SaveChangesAsync();
        db.Orders.RemoveRange(d1, d2);
        await db.SaveChangesAsync();

        var results = await db.Orders.OnlyDeleted().ToListAsync();
        results.Should().HaveCount(2);
        results.Should().NotContain(o => o.Name == "Active");
    }
}
