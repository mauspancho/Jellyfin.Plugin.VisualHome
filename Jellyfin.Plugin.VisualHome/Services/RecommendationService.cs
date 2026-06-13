using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.VisualHome.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.VisualHome.Services;

/// <summary>
/// Basic recommendation service based only on local Jellyfin data.
/// </summary>
public sealed class RecommendationService
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserDataManager _userDataManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationService"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userDataManager">User data manager.</param>
    public RecommendationService(ILibraryManager libraryManager, IUserDataManager userDataManager)
    {
        _libraryManager = libraryManager;
        _userDataManager = userDataManager;
    }

    /// <summary>
    /// Gets local recommendations for a user.
    /// </summary>
    /// <param name="user">Current user.</param>
    /// <param name="section">Section configuration.</param>
    /// <returns>Recommended base items.</returns>
    public IReadOnlyList<BaseItem> GetRecommendations(User user, VisualSectionConfig section)
    {
        var itemTypes = SectionQueryService.ParseItemTypes(section.ItemTypes);
        var recentPlayed = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                Recursive = true,
                IncludeItemTypes = itemTypes,
                IsPlayed = true,
                Limit = 300
            })
            .OrderByDescending(i => _userDataManager.GetUserData(user, i)?.LastPlayedDate ?? DateTime.MinValue)
            .Take(30)
            .ToList();

        var preferredGenres = recentPlayed
            .SelectMany(i => i.Genres ?? [])
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        var query = new InternalItemsQuery(user)
        {
            Recursive = true,
            IncludeItemTypes = itemTypes,
            IsPlayed = false,
            MinCommunityRating = section.MinCommunityRating,
            Limit = 600
        };

        if (preferredGenres.Count > 0)
        {
            query.Genres = preferredGenres;
        }

        var candidates = _libraryManager.GetItemList(query);
        if (candidates.Count == 0 && preferredGenres.Count > 0)
        {
            query.Genres = [];
            candidates = _libraryManager.GetItemList(query);
        }

        return candidates
            .OrderByDescending(i => i.CommunityRating ?? 0)
            .ThenByDescending(i => i.DateCreated)
            .Take(Math.Clamp(section.Limit, 1, 100))
            .ToList();
    }
}
