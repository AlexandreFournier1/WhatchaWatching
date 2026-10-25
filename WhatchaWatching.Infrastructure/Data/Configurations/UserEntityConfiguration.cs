using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Infrastructure.Data.Configurations;

/// <summary>
/// Only configures what Identity does not: the audit timestamps, the unique email index (the watchlist relationship is configured in <see cref="WatchListItemConfiguration"/>).
/// Username, email and password columns, and their unique indexes, are configured by <see cref="Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext{TUser, TRole, TKey}"/>.
/// </summary>
public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.ShowAdultContent)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();

        // Identity only makes the username index unique; enforce unique emails in the database too.
        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();
    }
}
