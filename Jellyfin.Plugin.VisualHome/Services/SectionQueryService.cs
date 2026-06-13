using System.Globalization;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.VisualHome.Configuration;
using Jellyfin.Plugin.VisualHome.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.VisualHome.Services;

/// <summary>
/// Queries Jellyfin library items for configured visual sections.
/// </summary>
public sealed class SectionQueryService
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserDataManager _userDataManager;
    private readonly RecommendationService _recommendationService;
    private readonly ILogger<SectionQueryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionQueryService"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userDataManager">User data manager.</param>
    /// <param name="recommendationService">Recommendation service.</param>
    /// <param name="logger">Logger.</param>
    public SectionQueryService(
        ILibraryManager libraryManager,
        IUserDataManager userDataManager,
        RecommendationService recommendationService,
        ILogger<SectionQueryService> logger)
    {
        _libraryManager = libraryManager;
        _userDataManager = userDataManager;
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// Builds a renderable section.
    /// </summary>
    /// <param name="section">Section configuration.</param>
    /// <param name="configuration">Plugin configuration.</param>
    /// <param name="user">Current user.</param>
    /// <returns>Renderable section.</returns>
    public VisualSectionResult BuildSection(VisualSectionConfig section, VisualHomeConfiguration configuration, User user)
    {
        try
        {
            if (!IsVisibleToUser(section, user))
            {
                return Empty(section);
            }

            var items = string.Equals(section.SortBy, "Recommendation", StringComparison.OrdinalIgnoreCase)
                ? _recommendationService.GetRecommendations(user, section)
                : QueryItems(section);

            var dtoItems = ApplySectionFilters(items, section, user)
                .Take(Math.Clamp(section.Limit, 1, 100))
                .Select(item => ToDto(item, user))
                .ToList();

            if (string.Equals(section.VisualType, "studioCollection", StringComparison.OrdinalIgnoreCase)
                && dtoItems.Count == 0)
            {
                dtoItems = BuildStudioFallbacks(configuration);
            }

            return new VisualSectionResult
            {
                SectionId = section.Id,
                Name = section.Name,
                VisualType = section.VisualType,
                Position = section.Position,
                Items = dtoItems,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VisualHome] Section {SectionId} failed", section.Id);
            return new VisualSectionResult
            {
                SectionId = section.Id,
                Name = section.Name,
                VisualType = section.VisualType,
                Position = section.Position,
                Items = [],
                Success = false,
                ErrorMessage = "Section failed. Check Jellyfin logs for [VisualHome]."
            };
        }
    }

    /// <summary>
    /// Parses configured item types.
    /// </summary>
    /// <param name="itemTypes">String item type values.</param>
    /// <returns>Base item kinds.</returns>
    public static BaseItemKind[] ParseItemTypes(IReadOnlyCollection<string> itemTypes)
    {
        var parsed = itemTypes
            .Select(type => string.Equals(type, "Series", StringComparison.OrdinalIgnoreCase) ? "Series" : type)
            .Select(type => Enum.TryParse<BaseItemKind>(type, true, out var kind) ? kind : (BaseItemKind?)null)
            .Where(kind => kind.HasValue)
            .Select(kind => kind!.Value)
            .Distinct()
            .ToArray();

        return parsed.Length == 0 ? [BaseItemKind.Movie, BaseItemKind.Series] : parsed;
    }

    private IReadOnlyList<BaseItem> QueryItems(VisualSectionConfig section)
    {
        var query = new InternalItemsQuery
        {
            Recursive = true,
            IncludeItemTypes = ParseItemTypes(section.ItemTypes),
            Genres = section.Genres,
            OfficialRatings = section.OfficialRatings.ToArray(),
            Tags = section.Tags.ToArray(),
            IsFavorite = section.IsFavorite,
            IsPlayed = section.IsPlayed,
            IsResumable = section.IsResumable,
            MinCommunityRating = section.MinCommunityRating,
            Limit = QueryLimit(section)
        };

        var topParentIds = ParseGuids(section.LibraryIds);
        if (topParentIds.Length > 0)
        {
            query.TopParentIds = topParentIds;
        }

        if (section.YearFrom.HasValue && section.YearTo.HasValue && section.YearFrom == section.YearTo)
        {
            query.Years = [section.YearFrom.Value];
        }

        if (section.AddedWithinDays.HasValue)
        {
            query.MinDateCreated = DateTime.UtcNow.AddDays(-Math.Abs(section.AddedWithinDays.Value));
        }

        if (section.PremiereDateFrom.HasValue)
        {
            query.MinPremiereDate = section.PremiereDateFrom.Value;
        }

        if (section.PremiereDateTo.HasValue)
        {
            query.MaxPremiereDate = section.PremiereDateTo.Value;
        }

        var studioIds = section.Studios
            .Select(TryGetStudioId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToArray();
        if (studioIds.Length > 0)
        {
            query.StudioIds = studioIds;
        }

        return _libraryManager.GetItemList(query);
    }

    private IEnumerable<BaseItem> ApplySectionFilters(IEnumerable<BaseItem> items, VisualSectionConfig section, User user)
    {
        var filtered = items;

        if (section.YearFrom.HasValue)
        {
            filtered = filtered.Where(item => (item.ProductionYear ?? 0) >= section.YearFrom.Value);
        }

        if (section.YearTo.HasValue)
        {
            filtered = filtered.Where(item => (item.ProductionYear ?? 0) <= section.YearTo.Value);
        }

        if (section.MaxRuntimeMinutes.HasValue)
        {
            var maxTicks = TimeSpan.FromMinutes(section.MaxRuntimeMinutes.Value).Ticks;
            filtered = filtered.Where(item => !item.RunTimeTicks.HasValue || item.RunTimeTicks.Value <= maxTicks);
        }

        filtered = ApplySort(filtered, section, user);
        return filtered;
    }

    private IEnumerable<BaseItem> ApplySort(IEnumerable<BaseItem> items, VisualSectionConfig section, User user)
    {
        var random = string.Equals(section.SortBy, "Random", StringComparison.OrdinalIgnoreCase);
        if (random)
        {
            return items.OrderBy(_ => Random.Shared.Next());
        }

        var sorted = section.SortBy switch
        {
            "PremiereDate" => items.OrderBy(i => i.PremiereDate ?? DateTime.MinValue),
            "CommunityRating" => items.OrderBy(i => i.CommunityRating ?? 0),
            "SortName" => items.OrderBy(i => i.SortName, StringComparer.OrdinalIgnoreCase),
            "PlayCount" => items.OrderBy(i => _userDataManager.GetUserData(user, i)?.PlayCount ?? 0),
            "LastPlayedDate" => items.OrderBy(i => _userDataManager.GetUserData(user, i)?.LastPlayedDate ?? DateTime.MinValue),
            "RuntimeTicks" => items.OrderBy(i => i.RunTimeTicks ?? long.MaxValue),
            "FavoritesFirst" => items
                .OrderBy(i => _userDataManager.GetUserData(user, i)?.IsFavorite == true ? 0 : 1)
                .ThenByDescending(i => i.CommunityRating ?? 0),
            _ => items.OrderBy(i => i.DateCreated)
        };

        return section.SortDescending && !string.Equals(section.SortBy, "SortName", StringComparison.OrdinalIgnoreCase)
            ? sorted.Reverse()
            : sorted;
    }

    private VisualHomeItemDto ToDto(BaseItem item, User user)
    {
        var id = item.Id.ToString("N", CultureInfo.InvariantCulture);
        var userData = _userDataManager.GetUserData(user, item);

        return new VisualHomeItemDto
        {
            Id = id,
            Name = item.Name,
            Type = item.GetType().Name,
            Overview = item.Overview,
            ProductionYear = item.ProductionYear,
            OfficialRating = item.OfficialRating,
            CommunityRating = item.CommunityRating.HasValue ? (double)item.CommunityRating.Value : null,
            RuntimeTicks = item.RunTimeTicks,
            ImagePrimaryUrl = $"/Items/{id}/Images/Primary",
            ImageBackdropUrl = $"/Items/{id}/Images/Backdrop/0",
            LogoUrl = $"/Items/{id}/Images/Logo",
            Genres = (item.Genres ?? []).ToList(),
            Studios = (item.Studios ?? []).ToList(),
            IsFavorite = userData?.IsFavorite == true,
            IsPlayed = userData?.Played == true,
            IsResumable = (userData?.PlaybackPositionTicks ?? 0) > 0,
            Url = $"#!/details?id={id}"
        };
    }

    private static bool IsVisibleToUser(VisualSectionConfig section, User user)
    {
        return section.VisibleUserIds.Count == 0
            || section.VisibleUserIds.Any(id => string.Equals(id, user.Id.ToString("N"), StringComparison.OrdinalIgnoreCase));
    }

    private static VisualSectionResult Empty(VisualSectionConfig section)
    {
        return new VisualSectionResult
        {
            SectionId = section.Id,
            Name = section.Name,
            VisualType = section.VisualType,
            Position = section.Position,
            Items = [],
            Success = true
        };
    }

    private static int QueryLimit(VisualSectionConfig section)
    {
        return Math.Clamp(section.Limit * 12, 100, 1000);
    }

    private static Guid[] ParseGuids(IEnumerable<string> values)
    {
        return values
            .Where(value => Guid.TryParse(value, out _))
            .Select(Guid.Parse)
            .ToArray();
    }

    private Guid? TryGetStudioId(string studio)
    {
        try
        {
            return string.IsNullOrWhiteSpace(studio) ? null : _libraryManager.GetStudioId(studio);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[VisualHome] Studio lookup failed for {Studio}", studio);
            return null;
        }
    }

    private static List<VisualHomeItemDto> BuildStudioFallbacks(VisualHomeConfiguration configuration)
    {
        return configuration.StudioCollections
            .Select(studio =>
            {
                var id = Uri.EscapeDataString(studio.Name);
                return new VisualHomeItemDto
                {
                    Id = id,
                    Name = studio.Name,
                    Type = "Studio",
                    ImagePrimaryUrl = studio.LogoUrl ?? string.Empty,
                    ImageBackdropUrl = studio.BackdropUrl ?? string.Empty,
                    LogoUrl = studio.LogoUrl ?? string.Empty,
                    Url = $"#!/search?query={id}",
                    Studios = [studio.Name]
                };
            })
            .ToList();
    }
}
