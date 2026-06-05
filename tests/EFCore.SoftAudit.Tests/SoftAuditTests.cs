using EFCore.SoftAudit.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit.Tests;

public sealed class SoftAuditTests
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
    public void ObsoleteConstructor_ShouldInitializeSuccessfully()
    {
#pragma warning disable CS0618 
        var options = new DbContextOptionsBuilder<ObsoleteTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };
        
#pragma warning disable CS0618 
        var context = new ObsoleteTestDbContext(options, httpContextAccessor);
#pragma warning restore CS0618

       
        Assert.NotNull(context);
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
    [Fact]
    public async Task Restore_ShouldClearDeletionFields_WhenEntityIsSoftDeleted()
    {
        await using var db = CreateDb(timeProvider: new FakeTimeProvider(FixedUtc));
        var order = new TestOrder { Name = "Test" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        order.IsDeleted.Should().BeTrue();
        order.DeletedAt.Should().Be(FixedUtc);
        db.Restore(order);
        order.IsDeleted.Should().BeFalse();
        order.DeletedAt.Should().BeNull();
        order.DeletedBy.Should().BeNull();
    }

    [Fact]
    public async Task Restore_ShouldThrowArgumentNullException_WhenEntityIsNull()
    {
        await using var db = CreateDb(timeProvider: new FakeTimeProvider(FixedUtc));
        var act = () => db.Restore<TestOrder>(null!);
        act.Should().Throw<ArgumentNullException>();
    }
    [Fact]
    public async Task Restore_ShouldBeVisibleInQuery_AfterSaveChanges()
    {
       await using var db = CreateDb(timeProvider: new FakeTimeProvider(FixedUtc));
       var order = new TestOrder { Name = "Test" };
       db.Orders.Add(order);
       await db.SaveChangesAsync();
       db.Orders.Remove(order);
       await db.SaveChangesAsync();
       var listOrders = await db.Orders.ToListAsync();
       listOrders.Should().BeEmpty();
       db.Restore(order);
       await db.SaveChangesAsync();
       var orders = await db.Orders.ToListAsync();
       orders.Should().HaveCount(1);
    }
    [Fact]
    public async Task Restore_ShouldSetUpdatedAuditFields_AfterSaveChanges()
    {
        await using var db = CreateDb(timeProvider: new FakeTimeProvider(FixedUtc),userProvider: new FakeUserProvider("user-123"));
        var order = new TestOrder { Name = "Test"};
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        order.UpdatedBy.Should().BeNull();
        order.UpdatedAt.Should().BeNull();
        db.Restore(order);
        await db.SaveChangesAsync();
        order.UpdatedAt.Should().Be(FixedUtc);
        order.UpdatedBy.Should().Be("user-123");
       
    }
    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnDeletedEntities()
    {
        await using var db = CreateDb(timeProvider: new FakeTimeProvider(FixedUtc));
        var order = new TestOrder { Name = "Test"};
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        var listOrders = await db.Orders.ToListAsync();
        listOrders.Should().BeEmpty();
        var orders = await db.Orders.IgnoreQueryFilters().ToListAsync();
        orders.Should().HaveCount(1);
    }
    [Fact]
    public async Task Delete_ShouldOnlyHideDeletedEntity_WhenMultipleExist()
    {
        await using var db = CreateDb(timeProvider: new FakeTimeProvider(FixedUtc));
        var order = new TestOrder { Name = "Test"};
        var order2 = new TestOrder { Name = "Test2"};
        db.Orders.Add(order);
        db.Orders.Add(order2);
        await db.SaveChangesAsync();
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        var listOrders = await db.Orders.ToListAsync();
        listOrders.Should().ContainSingle(o => o.Id == order2.Id);
    }
    [Fact]
    public async Task Add_ShouldNotFillUpdatedAt_WhenEntityCreated()
    {
        await using var db = CreateDb(timeProvider: new FakeTimeProvider(FixedUtc));
        var order = new TestOrder { Name = "Test"};
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        order.UpdatedAt.Should().BeNull();
        order.UpdatedBy.Should().BeNull();
    }
    
    [Fact]
    public async Task Add_ShouldNotFillCreatedBy_WhenUserProviderIsNull()
    {
        await using var db = CreateDb();
        var order = new TestOrder { Name = "Test"};
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        order.CreatedBy.Should().BeNull();
    }
    [Fact]
    public async Task Delete_ShouldNotFillUpdatedBy_WhenDeleted()
    {
        await using var db = CreateDb();
        var order = new TestOrder { Name = "Test"};
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        order.UpdatedBy.Should().BeNull();
        order.UpdatedAt.Should().BeNull();
    }
    [Fact]
    public async Task Restore_ShouldNotThrow_WhenEntityIsUntracked()
    {
        await using var db = CreateDb();
        var order = new TestOrder { Name = "Test", IsDeleted = true};
       var act = ()=> db.Restore(order);
       act.Should().NotThrow();
       order.IsDeleted.Should().BeFalse();

    }

    [Fact]
    public void ObsoleteConstructor_WithNullHttpContextAccessor_ShouldInitializeSuccessfully()
    {
#pragma warning disable CS0618
        var options = new DbContextOptionsBuilder<ObsoleteTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ObsoleteTestDbContext(options, null);
#pragma warning restore CS0618
        Assert.NotNull(context);
    }

    [Fact]
    public void Restore_ShouldNotThrow_WhenEntityIsNotDeleted()
    {
        using var db = CreateDb();
        var order = new TestOrder { Name = "Test", IsDeleted = false };
        var act = () => db.Restore(order);
        act.Should().NotThrow();
        order.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeletableOnly_Delete_ShouldSoftDeleteAndHideFromQuery()
    {
        await using var db = CreateDb(
            userProvider: new FakeUserProvider("user-123"),
            timeProvider: new FakeTimeProvider(FixedUtc));
        var entity = new TestSoftOnlyOrder { Name = "Test" };
        db.SoftOnlyOrders.Add(entity);
        await db.SaveChangesAsync();
        db.SoftOnlyOrders.Remove(entity);
        await db.SaveChangesAsync();
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(FixedUtc);
        entity.DeletedBy.Should().Be("user-123");
        var results = await db.SoftOnlyOrders.ToListAsync();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_AuditableOnlyEntity_ShouldHardDelete()
    {
        await using var db = CreateDb();
        var entity = new TestAuditOnlyOrder { Name = "Test" };
        db.AuditOnlyOrders.Add(entity);
        await db.SaveChangesAsync();
        db.AuditOnlyOrders.Remove(entity);
        await db.SaveChangesAsync();
        var results = await db.AuditOnlyOrders.ToListAsync();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task MixedOperations_ShouldApplyCorrectAuditRules_InSingleSaveChanges()
    {
        await using var db = CreateDb(
            userProvider: new FakeUserProvider("user-123"),
            timeProvider: new FakeTimeProvider(FixedUtc));
        var order1 = new TestOrder { Name = "Order1" };
        var order2 = new TestOrder { Name = "Order2" };
        db.Orders.AddRange(order1, order2);
        await db.SaveChangesAsync();

        order1.Name = "Order1-Updated";
        db.Orders.Remove(order2);
        await db.SaveChangesAsync();

        order1.UpdatedAt.Should().Be(FixedUtc);
        order1.UpdatedBy.Should().Be("user-123");
        order2.IsDeleted.Should().BeTrue();
        order2.DeletedAt.Should().Be(FixedUtc);
        order2.UpdatedAt.Should().BeNull();
        order2.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task RestoreRange_ShouldRestoreAllEntities()
    {
        await using var db = CreateDb(timeProvider: new FakeTimeProvider(FixedUtc));
        var order1 = new TestOrder { Name = "Order1" };
        var order2 = new TestOrder { Name = "Order2" };
        db.Orders.AddRange(order1, order2);
        await db.SaveChangesAsync();
        db.Orders.RemoveRange(order1, order2);
        await db.SaveChangesAsync();

        db.RestoreRange(new[] { order1, order2 });
        await db.SaveChangesAsync();

        var orders = await db.Orders.ToListAsync();
        orders.Should().HaveCount(2);
        order1.IsDeleted.Should().BeFalse();
        order2.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreRange_ShouldSetUpdatedAuditFields_ForAllEntities()
    {
        await using var db = CreateDb(
            userProvider: new FakeUserProvider("user-123"),
            timeProvider: new FakeTimeProvider(FixedUtc));
        var order1 = new TestOrder { Name = "Order1" };
        var order2 = new TestOrder { Name = "Order2" };
        db.Orders.AddRange(order1, order2);
        await db.SaveChangesAsync();
        db.Orders.RemoveRange(order1, order2);
        await db.SaveChangesAsync();

        db.RestoreRange(new[] { order1, order2 });
        await db.SaveChangesAsync();

        order1.UpdatedAt.Should().Be(FixedUtc);
        order1.UpdatedBy.Should().Be("user-123");
        order2.UpdatedAt.Should().Be(FixedUtc);
        order2.UpdatedBy.Should().Be("user-123");
    }

    [Fact]
    public void RestoreRange_ShouldThrowArgumentNullException_WhenCollectionIsNull()
    {
        using var db = CreateDb();
        var act = () => db.RestoreRange<TestOrder>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RestoreRange_ShouldNotThrow_WhenCollectionIsEmpty()
    {
        using var db = CreateDb();
        var act = () => db.RestoreRange(Array.Empty<TestOrder>());
        act.Should().NotThrow();
    }

}