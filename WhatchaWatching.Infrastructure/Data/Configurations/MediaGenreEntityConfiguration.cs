using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Infrastructure.Data.Configurations;

public class MediaGenreEntityConfiguration : IEntityTypeConfiguration<MediaGenreEntity>
{
    public void Configure(EntityTypeBuilder<MediaGenreEntity> builder)
    {
        builder.HasKey(mg => new { mg.MediaId, mg.GenreId });

        builder.HasOne(mg => mg.Media)
            .WithMany(m => m.MediaGenres)
            .HasForeignKey(mg => mg.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mg => mg.Genre)
            .WithMany(g => g.MediaGenres)
            .HasForeignKey(mg => mg.GenreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
