# Infrastructure (`WhatchaWatching.Infrastructure`)

EF Core `ApplicationDbContext`, entities, configurations, migrations. Repo-wide rules are in the root [CLAUDE.md](../CLAUDE.md).

## Database

MySQL through **EF Core** with the **Pomelo.EntityFrameworkCore.MySql** provider. Production runs on **AlwaysData** (MySQL).

- No SQL Server, PostgreSQL or SQLite specifics unless explicitly requested.
- Schema changes go through **EF Core migrations**, never by editing the database by hand.
- Data access lives in Application services (via `IDbContextFactory<ApplicationDbContext>`), never in Blazor components. Blazor Server needs short-lived contexts: create one per operation from the factory.
- Query only what is needed (`Select` projections, `AsNoTracking` for reads). Add indexes, foreign keys and constraints deliberately, as in the existing configurations.
- Entities that implement `IAuditableEntity` get their timestamps set by `ApplicationDbContext`; do not set them manually.
- Entities are `XxxEntity` in `Data/Entities`, configurations `XxxEntityConfiguration : IEntityTypeConfiguration<XxxEntity>` in `Data/Configurations`, migrations in `Migrations`. Column sizes come from `ConfigurationConstants`, names from `DatabaseConstants`.

## Migrations workflow

The design-time factory (`ApplicationDbContextFactory`) lets the EF tools run from this project alone, and it needs no live database.

1. Change the entity and its configuration.
2. Create the migration (run from the repo root):
   `dotnet ef migrations add <PascalCaseName> --project WhatchaWatching.Infrastructure`
3. Read the generated migration to confirm it matches the intent (no unexpected drops).
4. Update [docs/DATA_MODEL.md](../docs/DATA_MODEL.md) so it matches the model.
5. Run `dotnet build` and `dotnet test`.

Rules:
- Never hand-edit `ApplicationDbContextModelSnapshot.cs` or `*.Designer.cs`: they are generated (a hook blocks the edit).
- Never edit a migration that was already applied or pushed: add a new one.
- To discard a migration that was never applied, use `dotnet ef migrations remove --project WhatchaWatching.Infrastructure` (do not delete files by hand).
- Do not run `dotnet ef database update` against the production database (denied in `.claude/settings.json`). Applying migrations to production is the user's decision, through the `/apply-migrations` skill.
