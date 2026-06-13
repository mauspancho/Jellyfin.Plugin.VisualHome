using Jellyfin.Plugin.VisualHome.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.VisualHome.Configuration;

/// <summary>
/// Persistent plugin configuration.
/// </summary>
public sealed class VisualHomeConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VisualHomeConfiguration"/> class.
    /// </summary>
    public VisualHomeConfiguration()
    {
        PluginEnabled = true;
        VisualInjectionEnabled = true;
        SidebarEnabled = false;
        Sections = CreateDefaultSections();
        StudioCollections = CreateDefaultStudios();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin should serve sections.
    /// </summary>
    public bool PluginEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frontend should render visual sections.
    /// </summary>
    public bool VisualInjectionEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the optional right sidebar is enabled.
    /// </summary>
    public bool SidebarEnabled { get; set; }

    /// <summary>
    /// Gets or sets the configured visual sections.
    /// </summary>
    public List<VisualSectionConfig> Sections { get; set; }

    /// <summary>
    /// Gets or sets studio shortcut definitions.
    /// </summary>
    public List<StudioCollectionConfig> StudioCollections { get; set; }

    /// <summary>
    /// Creates the default section list.
    /// </summary>
    /// <returns>Default section list.</returns>
    public static List<VisualSectionConfig> CreateDefaultSections()
    {
        return
        [
            new VisualSectionConfig
            {
                Id = "hero-main",
                Name = "Hero principal",
                Enabled = true,
                VisualType = "hero",
                Position = 0,
                ItemTypes = ["Movie", "Series"],
                SortBy = "Random",
                Limit = 1,
                CacheMinutes = 5
            },
            new VisualSectionConfig
            {
                Id = "recent-movies",
                Name = "Peliculas anadidas recientemente",
                Enabled = true,
                VisualType = "carousel",
                Position = 10,
                ItemTypes = ["Movie"],
                SortBy = "DateCreated",
                SortDescending = true,
                Limit = 20,
                CacheMinutes = 5
            },
            new VisualSectionConfig
            {
                Id = "recent-series",
                Name = "Series anadidas recientemente",
                Enabled = true,
                VisualType = "carousel",
                Position = 20,
                ItemTypes = ["Series"],
                SortBy = "DateCreated",
                SortDescending = true,
                Limit = 20,
                CacheMinutes = 5
            },
            new VisualSectionConfig
            {
                Id = "top-movies",
                Name = "Top 10 peliculas",
                Enabled = true,
                VisualType = "top10",
                Position = 30,
                ItemTypes = ["Movie"],
                SortBy = "CommunityRating",
                SortDescending = true,
                Limit = 10,
                CacheMinutes = 30
            },
            new VisualSectionConfig
            {
                Id = "top-series",
                Name = "Top 10 series",
                Enabled = true,
                VisualType = "top10",
                Position = 40,
                ItemTypes = ["Series"],
                SortBy = "CommunityRating",
                SortDescending = true,
                Limit = 10,
                CacheMinutes = 30
            },
            new VisualSectionConfig
            {
                Id = "recommendations",
                Name = "Recomendaciones personalizadas",
                Enabled = true,
                VisualType = "carousel",
                Position = 50,
                ItemTypes = ["Movie", "Series"],
                IsPlayed = false,
                MinCommunityRating = 6.5,
                SortBy = "Recommendation",
                SortDescending = true,
                Limit = 20,
                CacheMinutes = 15
            },
            new VisualSectionConfig
            {
                Id = "studios",
                Name = "Colecciones de estudios",
                Enabled = true,
                VisualType = "studioCollection",
                Position = 60,
                ItemTypes = ["Movie", "Series"],
                SortBy = "SortName",
                Limit = 10,
                CacheMinutes = 720
            },
            new VisualSectionConfig
            {
                Id = "family",
                Name = "Para ver en familia",
                Enabled = true,
                VisualType = "carousel",
                Position = 70,
                ItemTypes = ["Movie", "Series"],
                Genres = ["Animacion", "Familiar", "Aventura", "Animation", "Family", "Adventure"],
                OfficialRatings = ["G", "PG", "TV-Y", "TV-PG", "MX-AA", "MX-A", "MX-B"],
                SortBy = "Random",
                Limit = 20,
                CacheMinutes = 30
            },
            new VisualSectionConfig
            {
                Id = "horror-night",
                Name = "Terror para la noche",
                Enabled = true,
                VisualType = "carousel",
                Position = 80,
                ItemTypes = ["Movie", "Series"],
                Genres = ["Horror", "Terror"],
                SortBy = "Random",
                Limit = 20,
                CacheMinutes = 30
            },
            new VisualSectionConfig
            {
                Id = "short-movies",
                Name = "Peliculas cortas",
                Enabled = true,
                VisualType = "carousel",
                Position = 90,
                ItemTypes = ["Movie"],
                MaxRuntimeMinutes = 100,
                SortBy = "CommunityRating",
                SortDescending = true,
                Limit = 20,
                CacheMinutes = 30
            }
        ];
    }

    /// <summary>
    /// Creates default studio shortcuts.
    /// </summary>
    /// <returns>Default studios.</returns>
    public static List<StudioCollectionConfig> CreateDefaultStudios()
    {
        return
        [
            new StudioCollectionConfig { Name = "Marvel Studios" },
            new StudioCollectionConfig { Name = "Pixar" },
            new StudioCollectionConfig { Name = "Walt Disney Pictures" },
            new StudioCollectionConfig { Name = "Warner Bros." },
            new StudioCollectionConfig { Name = "Lucasfilm" },
            new StudioCollectionConfig { Name = "DreamWorks" },
            new StudioCollectionConfig { Name = "Universal Pictures" },
            new StudioCollectionConfig { Name = "Paramount" },
            new StudioCollectionConfig { Name = "Sony Pictures" },
            new StudioCollectionConfig { Name = "Studio Ghibli" }
        ];
    }
}
