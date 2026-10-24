using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Infrastructure.Data.Configurations;

public class WatchListItemConfiguration : IEntityTypeConfiguration<WatchListItemEntity>
{
    public void Configure(EntityTypeBuilder<WatchListItemEntity> builder)
    {
        builder.HasKey(wli => wli.Id);

        builder.Property(wli => wli.UserId)
            .IsRequired();

        builder.Property(wli => wli.MediaId)
            .IsRequired();

        builder.Property(wli => wli.AddedAt)
            .IsRequired();

        builder.Property(wli => wli.UpdatedAt)
            .IsRequired();

        // A media can appear only once in a user's watchlist.
        builder.HasIndex(wli => new { wli.UserId, wli.MediaId })
            .IsUnique();

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
