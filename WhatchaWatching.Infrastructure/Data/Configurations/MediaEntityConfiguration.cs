using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Infrastructure.Data.Configurations;

public class MediaEntityConfiguration : IEntityTypeConfiguration<MediaEntity>
{
    public void Configure(EntityTypeBuilder<MediaEntity> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ExternalId)
            .IsRequired();

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.SMALL_SIZE)
            .IsRequired();

        builder.Property(m => m.Title)
            .HasMaxLength(ConfigurationConstants.LARGE_SIZE)
            .IsRequired();

        builder.Property(m => m.OriginalTitle)
            .HasMaxLength(ConfigurationConstants.LARGE_SIZE)
            .IsRequired(false);

        builder.Property(m => m.Synopsis)
            .HasMaxLength(ConfigurationConstants.XXXL_SIZE)
            .IsRequired(false);

        builder.Property(m => m.ReleaseDate)
            .IsRequired(false);

        builder.Property(m => m.EpisodeCount)
            .IsRequired(false);

        builder.Property(m => m.EpisodeDurationMinutes)
            .IsRequired(false);

        builder.Property(m => m.SeasonCount)
            .IsRequired(false);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.SMALL_SIZE)
            .IsRequired();

        builder.Property(m => m.Score)
            .HasPrecision(4, 2)
            .IsRequired(false);

        builder.Property(m => m.Year)
            .IsRequired(false);

        builder.Property(m => m.Season)
            .HasMaxLength(ConfigurationConstants.SMALL_SIZE)
            .IsRequired(false);

        builder.Property(m => m.Rating)
            .HasMaxLength(ConfigurationConstants.SMALL_SIZE)
            .IsRequired(false);

        builder.Property(m => m.ImageUrl)
            .HasMaxLength(ConfigurationConstants.LARGE_SIZE)
            .IsRequired(false);

        builder.Property(m => m.LastSyncedAt)
            .IsRequired(false);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        // A provider id is only unique within a media type (anime and manga ids overlap).
        builder.HasIndex(m => new { m.Type, m.ExternalId })
            .IsUnique();

        builder.HasIndex(m => m.Type);

        builder.HasIndex(m => m.Status);
    }
}
