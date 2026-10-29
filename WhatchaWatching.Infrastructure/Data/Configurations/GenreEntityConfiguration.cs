using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Infrastructure.Data.Configurations;

public class GenreEntityConfiguration : IEntityTypeConfiguration<GenreEntity>
{
    public void Configure(EntityTypeBuilder<GenreEntity> builder)
    {
        builder.HasKey(g => g.Id);

        // The genres stored before TMDB all come from MyAnimeList: the default gives them their source.
        builder.Property(g => g.Source)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.SMALL_SIZE)
            .HasDefaultValue(GenreSource.MyAnimeList)
            .IsRequired();

        builder.Property(g => g.ExternalId)
            .IsRequired();

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(ConfigurationConstants.LARGE_SIZE);

        // MyAnimeList and TMDB share names (Comedy, Drama) and ids (14 is Horror on one, Fantasy on the other):
        // only the pair is unique. The name is not, so that the watchlist filter can group both under one entry.
        builder.HasIndex(g => new { g.Source, g.ExternalId })
            .IsUnique();
    }
}
