using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Infrastructure.Data.Configurations;

public class StreamingLinkEntityConfiguration : IEntityTypeConfiguration<StreamingLinkEntity>
{
    public void Configure(EntityTypeBuilder<StreamingLinkEntity> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.MediaId)
            .IsRequired();

        builder.Property(l => l.Site)
            .HasMaxLength(ConfigurationConstants.SMALL_SIZE)
            .IsRequired();

        builder.Property(l => l.Url)
            .HasMaxLength(ConfigurationConstants.LARGE_SIZE)
            .IsRequired();

        // The links go away with their anime. The foreign key's index serves the lookup by anime.
        builder.HasOne(l => l.Media)
            .WithMany(m => m.StreamingLinks)
            .HasForeignKey(l => l.MediaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
