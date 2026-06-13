namespace Jellyfin.Plugin.VisualHome.Models;

/// <summary>
/// Lightweight item payload for Jellyfin Web.
/// </summary>
public sealed class VisualHomeItemDto
{
    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Jellyfin item type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overview.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? ProductionYear { get; set; }

    /// <summary>
    /// Gets or sets the official rating.
    /// </summary>
    public string? OfficialRating { get; set; }

    /// <summary>
    /// Gets or sets the community rating.
    /// </summary>
    public double? CommunityRating { get; set; }

    /// <summary>
    /// Gets or sets the runtime in ticks.
    /// </summary>
    public long? RuntimeTicks { get; set; }

    /// <summary>
    /// Gets or sets the primary image URL.
    /// </summary>
    public string ImagePrimaryUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backdrop image URL.
    /// </summary>
    public string ImageBackdropUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logo image URL.
    /// </summary>
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    public List<string> Genres { get; set; } = [];

    /// <summary>
    /// Gets or sets the studios.
    /// </summary>
    public List<string> Studios { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the current user marked the item favorite.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user played the item.
    /// </summary>
    public bool IsPlayed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user can resume the item.
    /// </summary>
    public bool IsResumable { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin Web details URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
}
