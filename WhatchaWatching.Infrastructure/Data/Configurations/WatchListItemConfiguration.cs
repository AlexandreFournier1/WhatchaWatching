using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Infrastructure.Data.Configurations;

public class WatchListItemConfiguration : IEntityTypeConfiguration<WatchListItemEntity>
{
    public void Configure(EntityTypeBuilder<WatchListItemEntity> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_WatchListItems_UserScore",
            "UserScore IS NULL OR UserScore BETWEEN 1 AND 10"));

        builder.HasKey(wli => wli.Id);

        builder.Property(wli => wli.UserId)
            .IsRequired();

        builder.Property(wli => wli.MediaId)
            .IsRequired();

        builder.Property(wli => wli.Status)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.SMALL_SIZE)
            .IsRequired();

        builder.Property(wli => wli.Progress)
            .IsRequired(false);

        builder.Property(wli => wli.UserScore)
            .IsRequired(false);

        builder.Property(wli => wli.Notes)
            .HasMaxLength(ConfigurationConstants.XXL_SIZE)
            .IsRequired(false);

        builder.Property(wli => wli.StartDate)
            .IsRequired(false);

        builder.Property(wli => wli.EndDate)
            .IsRequired(false);

        builder.Property(wli => wli.AddedAt)
            .IsRequired();

        builder.Property(wli => wli.UpdatedAt)
            .IsRequired();

        // A media can appear only once in a user's watchlist.
        builder.HasIndex(wli => new { wli.UserId, wli.MediaId })
            .IsUnique();

        // Random picker: items of a user by status.
        builder.HasIndex(wli => new { wli.UserId, wli.Status });

        builder.HasOne(wli => wli.User)
            .WithMany(u => u.WatchListItems)
            .HasForeignKey(wli => wli.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Media is shared catalog data: it cannot be removed while someone lists it.
        builder.HasOne(wli => wli.Media)
            .WithMany(m => m.WatchListItems)
            .HasForeignKey(wli => wli.MediaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
