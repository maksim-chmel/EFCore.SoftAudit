# EFCore.SoftAudit

A lightweight extension for [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) that adds **audit fields** and **soft delete** support with minimal setup.

Instead of wiring up `SaveChanges` interceptors and global query filters by hand, inherit from `AuditableDbContext`, implement two marker interfaces on your entities, and register the context via `AddSoftAudit`.

## Features

- **Automatic audit fields** on create and update (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`)
- **Soft delete** — `Remove()` sets `IsDeleted = true` instead of deleting the row
- **Global query filter** — soft-deleted entities are excluded from queries by default
- **Current user tracking** via `IHttpContextAccessor` and `ClaimTypes.NameIdentifier`
- **UTC timestamps** for all audit and delete fields

## Requirements

- .NET 8.0
- Entity Framework Core 8.0

## Project structure

```
EFCore.SoftAudit/
├── EFCore.SoftAudit.sln
├── EFCore.SoftAudit.csproj          # Library
├── AuditableDbContext.cs
├── ServiceCollectionExtensions.cs
├── Interfaces/
│   ├── IAuditable.cs
│   └── ISoftDeletable.cs
├── samples/
│   └── SampleApi/                   # Demo ASP.NET Core API
└── tests/
    └── EFCore.SoftAudit.Tests/      # Unit tests (xUnit)
```

## Quick start

### 1. Reference the library

```xml
<ProjectReference Include="path/to/EFCore.SoftAudit.csproj" />
```

### 2. Implement interfaces on your entity

```csharp
using EFCore.SoftAudit.Interfaces;

public class Order : IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Quantity { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### 3. Create a DbContext

```csharp
using EFCore.SoftAudit;
using Microsoft.EntityFrameworkCore;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IHttpContextAccessor? httpContextAccessor)
    : AuditableDbContext(options, httpContextAccessor)
{
    public DbSet<Order> Orders => Set<Order>();
}
```

### 4. Register in DI

```csharp
using EFCore.SoftAudit;

builder.Services.AddSoftAudit<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));
```

`AddSoftAudit` registers `IHttpContextAccessor` and your `DbContext` in one call.

## How it works

### Audit fields (`IAuditable`)

| Event   | Fields set                          |
|---------|-------------------------------------|
| Insert  | `CreatedAt`, `CreatedBy`            |
| Update  | `UpdatedAt`, `UpdatedBy`            |

`CreatedBy` / `UpdatedBy` are populated from the authenticated user's `NameIdentifier` claim when an HTTP context is available. Outside of a web request (e.g. in tests), these fields remain `null`.

### Soft delete (`ISoftDeletable`)

Calling `DbSet.Remove(entity)` does **not** issue a SQL `DELETE`. Instead, the entity is marked as modified with:

- `IsDeleted = true`
- `DeletedAt = <UTC now>`
- `DeletedBy = <current user>`

A global query filter (`IsDeleted == false`) is applied automatically to every entity that implements `ISoftDeletable`, so deleted rows are hidden from normal queries.

To include soft-deleted records explicitly:

```csharp
var allOrders = await db.Orders.IgnoreQueryFilters().ToListAsync();
```

## Sample API

The `samples/SampleApi` project is a minimal REST API demonstrating the library with SQLite.

```bash
dotnet run --project samples/SampleApi/SampleApi.csproj
```

Swagger UI is available at `https://localhost:7009/swagger` in Development.

| Method | Endpoint         | Description              |
|--------|------------------|--------------------------|
| POST   | `/orders`        | Create an order          |
| GET    | `/orders`        | List active orders       |
| DELETE | `/orders/{id}`   | Soft-delete an order     |

## Running tests

```bash
dotnet test
```

Tests use an in-memory database and cover:

- `CreatedAt` is set on insert
- `UpdatedAt` is set on update
- `Remove()` sets `IsDeleted` and `DeletedAt` instead of deleting
- Soft-deleted entities are excluded from queries

## Building the solution

```bash
dotnet build EFCore.SoftAudit.sln
```

## License

MIT
