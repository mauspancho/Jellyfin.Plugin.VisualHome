namespace Jellyfin.Plugin.VisualHome.Models;

/// <summary>
/// Configures one visual home section.
/// </summary>
public sealed class VisualSectionConfig
{
    /// <summary>
    /// Gets or sets the section id.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the section display name.
    /// </summary>
    public string Name { get; set; } = "Nueva seccion";

    /// <summary>
    /// Gets or sets a value indicating whether this section is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the visual type.
    /// </summary>
    public string VisualType { get; set; } = "carousel";

    /// <summary>
    /// Gets or sets the section order.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets included top-level library ids.
    /// </summary>
    public List<string> LibraryIds { get; set; } = [];

    /// <summary>
    /// Gets or sets included item types.
    /// </summary>
    public List<string> ItemTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets genre filters.
    /// </summary>
    public List<string> Genres { get; set; } = [];

    /// <summary>
    /// Gets or sets official rating filters.
    /// </summary>
    public List<string> OfficialRatings { get; set; } = [];

    /// <summary>
    /// Gets or sets studio filters.
    /// </summary>
    public List<string> Studios { get; set; } = [];

    /// <summary>
    /// Gets or sets tag filters.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the start year.
    /// </summary>
    public int? YearFrom { get; set; }

    /// <summary>
    /// Gets or sets the end year.
    /// </summary>
    public int? YearTo { get; set; }

    /// <summary>
    /// Gets or sets the minimum community rating.
    /// </summary>
    public double? MinCommunityRating { get; set; }

    /// <summary>
    /// Gets or sets the maximum runtime in minutes.
    /// </summary>
    public int? MaxRuntimeMinutes { get; set; }

    /// <summary>
    /// Gets or sets favorite state filter.
    /// </summary>
    public bool? IsFavorite { get; set; }

    /// <summary>
    /// Gets or sets played state filter.
    /// </summary>
    public bool? IsPlayed { get; set; }

    /// <summary>
    /// Gets or sets resumable state filter.
    /// </summary>
    public bool? IsResumable { get; set; }

    /// <summary>
    /// Gets or sets a recent added-days filter.
    /// </summary>
    public int? AddedWithinDays { get; set; }

    /// <summary>
    /// Gets or sets the minimum premiere date.
    /// </summary>
    public DateTime? PremiereDateFrom { get; set; }

    /// <summary>
    /// Gets or sets the maximum premiere date.
    /// </summary>
    public DateTime? PremiereDateTo { get; set; }

    /// <summary>
    /// Gets or sets the sort key.
    /// </summary>
    public string SortBy { get; set; } = "DateCreated";

    /// <summary>
    /// Gets or sets a value indicating whether the sort is descending.
    /// </summary>
    public bool SortDescending { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum result count.
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Gets or sets cache lifetime in minutes.
    /// </summary>
    public int CacheMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets user ids that may see this section. Empty means all users.
    /// </summary>
    public List<string> VisibleUserIds { get; set; } = [];
}
