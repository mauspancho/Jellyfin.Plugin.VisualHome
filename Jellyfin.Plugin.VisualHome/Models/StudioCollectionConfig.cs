namespace Jellyfin.Plugin.VisualHome.Models;

/// <summary>
/// Configures a studio visual shortcut.
/// </summary>
public sealed class StudioCollectionConfig
{
    /// <summary>
    /// Gets or sets the studio name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional logo URL.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets an optional backdrop URL.
    /// </summary>
    public string? BackdropUrl { get; set; }
}
