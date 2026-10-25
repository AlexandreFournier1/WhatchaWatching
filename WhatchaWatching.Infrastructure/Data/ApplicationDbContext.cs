using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Infrastructure.Data;

// Users (UserEntity) and the Identity tables come from IdentityDbContext.
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<UserEntity, IdentityRole<int>, int>(options)
{
    public DbSet<MediaEntity> Media => Set<MediaEntity>();
    public DbSet<GenreEntity> Genres => Set<GenreEntity>();
    public DbSet<MediaGenreEntity> MediaGenres => Set<MediaGenreEntity>();
    public DbSet<StreamingLinkEntity> StreamingLinks => Set<StreamingLinkEntity>();
    public DbSet<WatchListItemEntity> WatchListItems => Set<WatchListItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Use the project's table names instead of the AspNet* defaults.
        modelBuilder.Entity<UserEntity>().ToTable("Users");
        modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Sets the UTC creation timestamp (<c>CreatedAt</c> or <c>AddedAt</c>) of new entities
    /// and the <c>UpdatedAt</c> timestamp of new and modified ones.
    /// </summary>
    private void ApplyTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Properties
                    .FirstOrDefault(p => p.Metadata.Name is "CreatedAt" or "AddedAt")
                    ?.CurrentValue = now;

                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
