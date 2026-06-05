# EFCore.SoftAudit

[![CI](https://github.com/maksim-chmel/EFCore.SoftAudit/actions/workflows/ci.yml/badge.svg)](https://github.com/maksim-chmel/EFCore.SoftAudit/actions/workflows/ci.yml)

A lightweight extension for [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) that adds **audit fields** and **soft delete** support with minimal setup.

Instead of wiring up `SaveChanges` interceptors and global query filters by hand, inherit from `AuditableDbContext`, implement two marker interfaces on your entities, and register the context via `AddSoftAudit`.

## Features

- **Automatic audit fields** on create and update (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`)
- **Soft delete** — `Remove()` sets `IsDeleted = true` instead of deleting the row
- **Global query filter** — soft-deleted entities are excluded from queries by default
- **Restore** — `Restore()` and `RestoreRange()` undo a soft delete and re-expose the entity to queries
- **Fluent query extensions** — `WithDeleted()` and `OnlyDeleted()` on any `IQueryable<T> where T : ISoftDeletable`
- **Pluggable user and time providers** via `ICurrentUserProvider` and `ITimeProvider`
- **Configurable claim type** — `HttpCurrentUserProvider` reads any claim you choose (default: `ClaimTypes.NameIdentifier`)
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
├── HttpCurrentUserProvider.cs
├── SystemTimeProvider.cs
├── ServiceCollectionExtensions.cs
├── SoftAuditOptions.cs
├── SoftDeleteQueryableExtensions.cs
├── Interfaces/
│   ├── IAuditable.cs
│   ├── ISoftDeletable.cs
│   ├── ICurrentUserProvider.cs
│   └── ITimeProvider.cs
├── samples/
│   └── SampleApi/                   # Demo ASP.NET Core API
└── tests/
    └── EFCore.SoftAudit.Tests/      # Unit tests (xUnit)
```

## Quick start

### 1. Install the package

```bash
dotnet add package EFCore.SoftAudit
```

Or manually in your `.csproj`:

```xml
<PackageReference Include="EFCore.SoftAudit" Version="1.2.0" />
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

You can implement either interface independently — an entity with only `ISoftDeletable` gets soft delete without audit fields; an entity with only `IAuditable` gets audit fields with normal (hard) delete.

### 3. Create a DbContext

```csharp
using EFCore.SoftAudit;
using EFCore.SoftAudit.Interfaces;
using Microsoft.EntityFrameworkCore;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserProvider? currentUserProvider,
    ITimeProvider? timeProvider)
    : AuditableDbContext(options, currentUserProvider, timeProvider)
{
    public DbSet<Order> Orders => Set<Order>();
}
```

### 4. Register in DI

```csharp
using EFCore.SoftAudit;

// Default — reads ClaimTypes.NameIdentifier from the current HTTP context
builder.Services.AddSoftAudit<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// Custom claim type (e.g. "sub" for OAuth2 / OpenID Connect)
builder.Services.AddSoftAudit<AppDbContext>(
    options => options.UseSqlite("Data Source=app.db"),
    audit => audit.UserClaimType = "sub");
```

`AddSoftAudit` registers:

- `IHttpContextAccessor`
- `ICurrentUserProvider` → `HttpCurrentUserProvider`
- `ITimeProvider` → `SystemTimeProvider`
- your `DbContext`

### Custom providers (console apps, tests, workers)

Register your own implementations before `AddDbContext`:

```csharp
services.AddSingleton<ITimeProvider, FakeTimeProvider>();
services.AddSingleton<ICurrentUserProvider, FakeUserProvider>();
services.AddDbContext<AppDbContext>(options => ...);
```

Or pass `null` providers to `AuditableDbContext` — timestamps fall back to `DateTime.UtcNow`, user fields remain `null`.

## How it works

### Audit fields (`IAuditable`)

| Event   | Fields set                          |
|---------|-------------------------------------|
| Insert  | `CreatedAt`, `CreatedBy`            |
| Update  | `UpdatedAt`, `UpdatedBy`            |

`CreatedBy` / `UpdatedBy` come from `ICurrentUserProvider.GetCurrentUserId()`. In ASP.NET Core apps this is populated from the configured claim via `HttpCurrentUserProvider`.

### Soft delete (`ISoftDeletable`)

Calling `DbSet.Remove(entity)` does **not** issue a SQL `DELETE`. Instead, the entity is marked as modified with:

- `IsDeleted = true`
- `DeletedAt = <UTC now>`
- `DeletedBy = <current user>`

A global query filter (`IsDeleted == false`) is applied automatically to every entity that implements `ISoftDeletable`, so deleted rows are hidden from normal queries.

### Querying soft-deleted entities

Use the fluent extensions from `EFCore.SoftAudit`:

```csharp
// Include deleted alongside active records
var allOrders = await db.Orders.WithDeleted().ToListAsync();

// Return only deleted records
var deletedOrders = await db.Orders.OnlyDeleted().ToListAsync();
```

> **Note:** `WithDeleted()` calls `IgnoreQueryFilters()` internally, which removes **all** global query filters on the entity type — not just the soft-delete filter. If you have additional filters (e.g. multi-tenancy), they will also be bypassed.

### Restoring soft-deleted entities

```csharp
// Restore a single entity
db.Restore(order);
await db.SaveChangesAsync();

// Restore multiple entities at once
db.RestoreRange(orders);
await db.SaveChangesAsync();
```

After restore, `IsDeleted`, `DeletedAt`, and `DeletedBy` are cleared. If the entity also implements `IAuditable`, `UpdatedAt` and `UpdatedBy` are stamped on the next `SaveChanges`.

Prefer `FirstOrDefaultAsync` over `FindAsync` when loading by id — `FindAsync` may return soft-deleted entities already tracked by the context.

## Limitations

- `ExecuteDelete()` and `ExecuteUpdate()` bypass `SaveChanges` — soft delete does not apply
- `UpdatedAt` is set on any `Modified` entity, even without business property changes
- `CreatedAt` is always overwritten on insert

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

Tests use an in-memory database and cover audit fields, soft delete, restore, sync/async `SaveChanges`, query extensions, and custom user/time providers.

## Building the solution

```bash
dotnet build EFCore.SoftAudit.sln
```

## License

MIT
