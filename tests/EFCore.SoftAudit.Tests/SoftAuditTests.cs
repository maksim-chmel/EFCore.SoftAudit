using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit.Tests;

public class SoftAuditTests
{
    private TestDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) 
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task Add_ShouldFillCreatedAt()
    {
        // Arrange
        using var db = CreateDb();
        var order = new TestOrder { Name = "Test" };

        // Act
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Assert
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
}