using System.ComponentModel.DataAnnotations;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Common.Validation;

namespace WhatchaWatching.Common.Dtos;

/// <summary>What a user types to add an anime that the providers do not have. Also the model of the form.</summary>
public class CustomAnimeDto
{
    [Required(ErrorMessage = "Enter the title.")]
    [StringLength(ConfigurationConstants.LARGE_SIZE, ErrorMessage = "The title is too long.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(ConfigurationConstants.LARGE_SIZE, ErrorMessage = "The original title is too long.")]
    public string? OriginalTitle { get; set; }

    [StringLength(ConfigurationConstants.XXXL_SIZE, ErrorMessage = "The summary is too long.")]
    public string? Synopsis { get; set; }

    [StringLength(ConfigurationConstants.LARGE_SIZE, ErrorMessage = "The image address is too long.")]
    [HttpUrl]
    public string? ImageUrl { get; set; }

    [Range(1, CustomAnimeConstants.MAX_EPISODES, ErrorMessage = "The number of episodes must be between 1 and 10000.")]
    public int? EpisodeCount { get; set; }

    [Range(1, CustomAnimeConstants.MAX_EPISODE_DURATION_MINUTES, ErrorMessage = "The duration must be between 1 and 600 minutes.")]
    public int? EpisodeDurationMinutes { get; set; }

    [Range(CustomAnimeConstants.MIN_YEAR, CustomAnimeConstants.MAX_YEAR, ErrorMessage = "The year must be between 1900 and 2100.")]
    public int? Year { get; set; }

    public MediaStatus Status { get; set; } = MediaStatus.Finished;

    /// <summary>Names from <see cref="GenreCatalog"/>.</summary>
    public List<string> Genres { get; set; } = [];
}
