namespace Jellyfin.Plugin.VisualHome.Models;

/// <summary>
/// Renderable section payload.
/// </summary>
public sealed class VisualSectionResult
{
    /// <summary>
    /// Gets or sets the section id.
    /// </summary>
    public string SectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the visual type.
    /// </summary>
    public string VisualType { get; set; } = "carousel";

    /// <summary>
    /// Gets or sets the section position.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets items in this section.
    /// </summary>
    public List<VisualHomeItemDto> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the section query succeeded.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets a controlled error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
