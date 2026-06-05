# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-06-05

### Added
- `AuditableDbContext.Restore<TEntity>(TEntity)` — clears `IsDeleted`, `DeletedAt`, and `DeletedBy` on a soft-deleted entity and marks it as modified so the next `SaveChanges` persists the restore. If the entity also implements `IAuditable`, `UpdatedAt` and `UpdatedBy` are stamped automatically.
- `AuditableDbContext.RestoreRange<TEntity>(IEnumerable<TEntity>)` — restores a collection of soft-deleted entities in a single call; delegates each item to `Restore()`.
- `SoftDeleteQueryableExtensions.WithDeleted<T>()` — fluent extension on `IQueryable<T> where T : ISoftDeletable` that includes soft-deleted entities in query results (calls `IgnoreQueryFilters()` internally).
- `SoftDeleteQueryableExtensions.OnlyDeleted<T>()` — fluent extension that returns only soft-deleted entities (`IgnoreQueryFilters()` + `Where(x => x.IsDeleted)`).
- `SoftAuditOptions` — configuration class with a `UserClaimType` property (default: `ClaimTypes.NameIdentifier`) for controlling which claim `HttpCurrentUserProvider` reads.
- `AddSoftAudit<TContext>()` now accepts an optional `Action<SoftAuditOptions>` parameter for configuring the claim type and future options without breaking existing call sites.
- `HttpCurrentUserProvider` now accepts an optional `string claimType` constructor parameter (default: `ClaimTypes.NameIdentifier`), making it usable standalone without `SoftAuditOptions`.

## [1.1.1] - 2026-06-01

> **Upgrading from v1.0.0?** Go straight to this version — skip v1.1.0 entirely.
> Your existing code will compile with a deprecation warning but **no changes required**.
>
> **Already on v1.1.0?** This is a safe, non-breaking patch. Nothing to change.

### Fixed
- Restored backward-compatible constructor `AuditableDbContext(DbContextOptions, IHttpContextAccessor?)` (marked `[Obsolete]`) to fix the unintentional breaking change introduced in v1.1.0.
  Users on v1.0.0 can upgrade to v1.1.1 without modifying their code.

### Deprecated
- `AuditableDbContext(DbContextOptions, IHttpContextAccessor?)` — still works, but migrate when convenient:
  1. Remove `IHttpContextAccessor` from your `DbContext` constructor.
  2. Call `AddSoftAudit<TContext>()` in DI — it registers `ICurrentUserProvider` and `ITimeProvider` automatically.

  ```csharp
  // Before (v1.0.0 — still compiles in v1.1.1 with a warning)
  public class MyContext(DbContextOptions<MyContext> options, IHttpContextAccessor acc)
      : AuditableDbContext(options, acc) { }

  // After (v1.1.x — recommended)
  public class MyContext(DbContextOptions<MyContext> options,
      ICurrentUserProvider userProvider, ITimeProvider timeProvider)
      : AuditableDbContext(options, userProvider, timeProvider) { }
  ```

## [1.1.0] - 2026-06-01

> ⚠️ **This version contains an unintentional breaking change.**
> Upgrading from v1.0.0 to v1.1.0 will break your build if your `DbContext` subclass
> passes `IHttpContextAccessor` to the base constructor.
> **Skip this version and upgrade directly to v1.1.1.**

### Added
- `ICurrentUserProvider` interface and `HttpCurrentUserProvider` implementation — replaces direct `IHttpContextAccessor` dependency in `AuditableDbContext`.
- `ITimeProvider` interface and `SystemTimeProvider` implementation — makes timestamp resolution pluggable and testable.
- `ServiceCollectionExtensions.AddSoftAudit<TContext>()` now automatically registers `ITimeProvider` (singleton) and `ICurrentUserProvider` (scoped).
- All four `SaveChanges` overloads are now covered: `SaveChanges()`, `SaveChanges(bool)`, `SaveChangesAsync(CancellationToken)`, `SaveChangesAsync(bool, CancellationToken)`.
- Soft-deleted entities are skipped during `UpdatedAt`/`UpdatedBy` assignment to prevent conflicting audit stamps.

### Changed
- `AuditableDbContext` primary constructor now accepts `ICurrentUserProvider?` and `ITimeProvider?` instead of `IHttpContextAccessor?`.
- Audit logic extracted into a private `ApplyAuditRules()` method shared across all `SaveChanges` overloads.
- Replaced `Microsoft.AspNetCore.Http` and `Microsoft.AspNetCore.Http.Abstractions` NuGet references with the `Microsoft.AspNetCore.App` framework reference.
- Replaced `Microsoft.Extensions.DependencyInjection` with the lighter `Microsoft.Extensions.DependencyInjection.Abstractions`.

### Removed
- Direct `IHttpContextAccessor` dependency from `AuditableDbContext` constructor (moved to `HttpCurrentUserProvider`).

## [1.0.0] - 2026-05-31

### Added
- `AuditableDbContext` — abstract `DbContext` base class with automatic soft delete and audit field population.
- `IAuditable` interface (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`).
- `ISoftDeletable` interface (`IsDeleted`, `DeletedAt`, `DeletedBy`).
- Global query filter on `ISoftDeletable` entities to exclude soft-deleted records from queries.
- `AddSoftAudit<TContext>()` extension method for `IServiceCollection`.
- Sample ASP.NET Core API demonstrating SQLite integration.
- MIT License.
- GitHub Actions CI workflow.

[1.2.0]: https://github.com/maksim-chmel/EFCore.SoftAudit/compare/v1.1.1...v1.2.0
[1.1.1]: https://github.com/maksim-chmel/EFCore.SoftAudit/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/maksim-chmel/EFCore.SoftAudit/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/maksim-chmel/EFCore.SoftAudit/releases/tag/v1.0.0
