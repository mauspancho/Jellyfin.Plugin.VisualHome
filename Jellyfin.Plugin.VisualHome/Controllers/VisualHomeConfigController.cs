using System.Reflection;
using System.Security.Claims;
using Jellyfin.Plugin.VisualHome.Configuration;
using Jellyfin.Plugin.VisualHome.Models;
using Jellyfin.Plugin.VisualHome.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.VisualHome.Controllers;

/// <summary>
/// Administrative endpoints for Visual Home.
/// </summary>
[ApiController]
[Authorize]
[Route("VisualHome/config")]
public sealed class VisualHomeConfigController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly SectionCacheService _cacheService;
    private readonly ILogger<VisualHomeConfigController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualHomeConfigController"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="cacheService">Cache service.</param>
    /// <param name="logger">Logger.</param>
    public VisualHomeConfigController(
        ILibraryManager libraryManager,
        IUserManager userManager,
        SectionCacheService cacheService,
        ILogger<VisualHomeConfigController> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Gets current plugin configuration.
    /// </summary>
    /// <returns>Current configuration.</returns>
    [HttpGet]
    public ActionResult<VisualHomeConfiguration> GetConfiguration()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return GetPluginConfiguration();
    }

    /// <summary>
    /// Saves plugin configuration.
    /// </summary>
    /// <param name="configuration">New configuration.</param>
    /// <returns>Saved configuration.</returns>
    [HttpPost]
    public ActionResult<VisualHomeConfiguration> SaveConfiguration([FromBody] VisualHomeConfiguration configuration)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        NormalizeConfiguration(configuration);
        VisualHomePlugin.Instance!.UpdateConfiguration(configuration);
        _cacheService.Clear();
        _logger.LogInformation("[VisualHome] Configuration saved");
        return configuration;
    }

    /// <summary>
    /// Lists available virtual libraries.
    /// </summary>
    /// <returns>Library list.</returns>
    [HttpGet("libraries")]
    public ActionResult<IEnumerable<object>> GetLibraries()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var libraries = _libraryManager.GetVirtualFolders()
            .Select(folder => new
            {
                Id = GetReflectedValue(folder, "ItemId") ?? GetReflectedValue(folder, "Id") ?? GetReflectedValue(folder, "Name"),
                Name = GetReflectedValue(folder, "Name") ?? "Library",
                CollectionType = GetReflectedValue(folder, "CollectionType") ?? string.Empty
            })
            .OrderBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return libraries;
    }

    /// <summary>
    /// Lists available genres.
    /// </summary>
    /// <returns>Genre list.</returns>
    [HttpGet("genres")]
    public ActionResult<IEnumerable<string>> GetGenres()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return GetAllLibraryItems()
            .SelectMany(item => item.Genres ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Lists available official ratings.
    /// </summary>
    /// <returns>Rating list.</returns>
    [HttpGet("official-ratings")]
    public ActionResult<IEnumerable<string>> GetOfficialRatings()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return GetAllLibraryItems()
            .Select(item => item.OfficialRating)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }

    /// <summary>
    /// Lists available studios.
    /// </summary>
    /// <returns>Studio list.</returns>
    [HttpGet("studios")]
    public ActionResult<IEnumerable<string>> GetStudios()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        return GetAllLibraryItems()
            .SelectMany(item => item.Studios ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Restores default sections and studios.
    /// </summary>
    /// <returns>Updated configuration.</returns>
    [HttpPost("restore-defaults")]
    public ActionResult<VisualHomeConfiguration> RestoreDefaults()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var configuration = GetPluginConfiguration();
        configuration.Sections = VisualHomeConfiguration.CreateDefaultSections();
        configuration.StudioCollections = VisualHomeConfiguration.CreateDefaultStudios();
        VisualHomePlugin.Instance!.UpdateConfiguration(configuration);
        _cacheService.Clear();
        _logger.LogInformation("[VisualHome] Default sections restored");
        return configuration;
    }

    /// <summary>
    /// Clears Visual Home cache.
    /// </summary>
    /// <returns>Status payload.</returns>
    [HttpPost("clear-cache")]
    public ActionResult<object> ClearCache()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        _cacheService.Clear();
        _logger.LogInformation("[VisualHome] Cache cleared");
        return new { Cleared = true };
    }

    private bool IsAdmin()
    {
        var userId = GetUserId(User);
        if (userId == Guid.Empty)
        {
            return false;
        }

        var user = _userManager.GetUserById(userId);
        var policy = user?.GetType().GetProperty("Policy", BindingFlags.Instance | BindingFlags.Public)?.GetValue(user);
        var isAdmin = policy?.GetType().GetProperty("IsAdministrator", BindingFlags.Instance | BindingFlags.Public)?.GetValue(policy);
        return isAdmin is true;
    }

    private static Guid GetUserId(ClaimsPrincipal claimsPrincipal)
    {
        foreach (var claim in claimsPrincipal.Claims)
        {
            if ((claim.Type.Contains("UserId", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(claim.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase))
                && Guid.TryParse(claim.Value, out var userId))
            {
                return userId;
            }
        }

        return Guid.Empty;
    }

    private static VisualHomeConfiguration GetPluginConfiguration()
    {
        return VisualHomePlugin.Instance?.Configuration ?? new VisualHomeConfiguration();
    }

    private IReadOnlyList<BaseItem> GetAllLibraryItems()
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            Recursive = true,
            IncludeItemTypes = SectionQueryService.ParseItemTypes(["Movie", "Series", "BoxSet"]),
            Limit = 5000
        });
    }

    private static void NormalizeConfiguration(VisualHomeConfiguration configuration)
    {
        configuration.Sections ??= [];
        configuration.StudioCollections ??= [];

        foreach (var section in configuration.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Id))
            {
                section.Id = Guid.NewGuid().ToString("N");
            }

            section.Name = string.IsNullOrWhiteSpace(section.Name) ? "Nueva seccion" : section.Name.Trim();
            section.VisualType = string.IsNullOrWhiteSpace(section.VisualType) ? "carousel" : section.VisualType.Trim();
            section.Limit = Math.Clamp(section.Limit, 1, 100);
            section.CacheMinutes = Math.Clamp(section.CacheMinutes, 0, 1440);
        }
    }

    private static string? GetReflectedValue(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(value)?.ToString();
    }
}
