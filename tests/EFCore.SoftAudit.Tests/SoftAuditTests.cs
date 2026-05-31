using EFCore.SoftAudit.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit.Tests;

public class SoftAuditTests
{
    private static readonly DateTime FixedUtc = new(2026, 5, 31, 12, 0, 0, DateTimeKind.Utc);

    private TestDbContext CreateDb(
        ICurrentUserProvider? userProvider = null,
        ITimeProvider? timeProvider = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options, userProvider, timeProvider);
    }

    [Fact]
    public async Task Add_ShouldFillCreatedAt()
    {
       await using var db = CreateDb();
       var order = new TestOrder { Name = "Test" };
       db.Orders.Add(order);
       await db.SaveChangesAsync();
       order.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Delete_ShouldSetIsDeleted()
    {
        await using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        order.IsDeleted.Should().BeTrue();
        order.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ShouldHideFromQuery()
    {
        await using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        var orders = await db.Orders.ToListAsync();
        orders.Should().BeEmpty();
    }
    [Fact]
    public async Task Update_ShouldFillUpdatedAt()
    {
        await using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var order2 = await db.Orders.FirstAsync();
        order2.Name = "Test2";
        await db.SaveChangesAsync();
        order2.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Delete_ShouldNotFillUpdatedAt()
    {
        await using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        var order2 = await db.Orders.IgnoreQueryFilters().FirstAsync();
        order2.UpdatedAt.Should().BeNull();
    }
    [Fact]
    public async Task Update_ShouldNotFillCreatedAt()
    {
        await using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await  db.SaveChangesAsync();
        var createdAt = order.CreatedAt;
        var order2 = await db.Orders.FirstAsync();
        order2.Name = "Test2";
        await db.SaveChangesAsync();
        order2.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Add_ShouldFillCreatedAt_Sync()
    {
        using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        db.SaveChanges();
        order.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public void Delete_SyncSaveChanges_ShouldSetIsDeleted()
    {
        using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        db.SaveChanges();
        db.Orders.Remove(order);
        db.SaveChanges();
        order.IsDeleted.Should().BeTrue();
        order.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_SyncSaveChangesWithAcceptAllChangesOnSuccess_ShouldSetIsDeleted()
    {
        using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        db.SaveChanges(acceptAllChangesOnSuccess: true);
        db.Orders.Remove(order);
        db.SaveChanges(acceptAllChangesOnSuccess: true);
        order.IsDeleted.Should().BeTrue();
        order.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_AsyncSaveChangesWithAcceptAllChangesOnSuccess_ShouldSetIsDeleted()
    {
        await using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync(acceptAllChangesOnSuccess: true);
        db.Orders.Remove(order);
        await db.SaveChangesAsync(acceptAllChangesOnSuccess: true);
        order.IsDeleted.Should().BeTrue();
        order.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Add_ShouldFillCreatedBy_WhenUserProviderIsRegistered()
    {
        await using var db = CreateDb(new FakeUserProvider("user-123"));
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        order.CreatedBy.Should().Be("user-123");
    }

    [Fact]
    public async Task Update_ShouldFillUpdatedBy_WhenUserProviderIsRegistered()
    {
        await using var db = CreateDb(new FakeUserProvider("user-123"));
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var tracked = await db.Orders.FirstAsync();
        tracked.Name = "Updated";
        await db.SaveChangesAsync();
        tracked.UpdatedBy.Should().Be("user-123");
    }

    [Fact]
    public async Task Delete_ShouldFillDeletedBy_WhenUserProviderIsRegistered()
    {
        await using var db = CreateDb(new FakeUserProvider("user-123"));
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        order.DeletedBy.Should().Be("user-123");
    }

    [Fact]
    public async Task Add_ShouldUseTimeProvider_WhenRegistered()
    {
        await using var db = CreateDb(timeProvider: new FakeTimeProvider(FixedUtc));
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        order.CreatedAt.Should().Be(FixedUtc);
    }
}