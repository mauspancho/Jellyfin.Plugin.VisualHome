using System.Reflection;
using System.Security.Claims;
using Jellyfin.Plugin.VisualHome.Configuration;
using Jellyfin.Plugin.VisualHome.Models;
using Jellyfin.Plugin.VisualHome.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Branding;
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
    private const string PluginVersion = "0.1.0.10";

    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly SectionCacheService _cacheService;
    private readonly ILogger<VisualHomeConfigController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualHomeConfigController"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="serverConfigurationManager">Server configuration manager.</param>
    /// <param name="cacheService">Cache service.</param>
    /// <param name="logger">Logger.</param>
    public VisualHomeConfigController(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IServerConfigurationManager serverConfigurationManager,
        SectionCacheService cacheService,
        ILogger<VisualHomeConfigController> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _serverConfigurationManager = serverConfigurationManager;
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

    /// <summary>
    /// Installs the Visual Home CSS import into Jellyfin custom branding CSS.
    /// </summary>
    /// <returns>Status payload.</returns>
    [HttpPost("install-css")]
    public ActionResult<object> InstallCustomCss()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var branding = _serverConfigurationManager.GetConfiguration<BrandingOptions>("branding");
        var currentCss = branding.CustomCss ?? string.Empty;
        var import = GetCssImport();
        if (!currentCss.Contains(import, StringComparison.OrdinalIgnoreCase))
        {
            branding.CustomCss = string.IsNullOrWhiteSpace(currentCss)
                ? import
                : currentCss.TrimEnd() + Environment.NewLine + Environment.NewLine + import;
            _serverConfigurationManager.SaveConfiguration("branding", branding);
        }

        _logger.LogInformation("[VisualHome] Installed Visual Home CSS import into Jellyfin branding CSS");
        return new { Installed = true, Import = import };
    }

    /// <summary>
    /// Removes the Visual Home CSS import from Jellyfin custom branding CSS.
    /// </summary>
    /// <returns>Status payload.</returns>
    [HttpPost("remove-css")]
    public ActionResult<object> RemoveCustomCss()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var branding = _serverConfigurationManager.GetConfiguration<BrandingOptions>("branding");
        var currentCss = branding.CustomCss ?? string.Empty;
        var import = GetCssImport();
        branding.CustomCss = currentCss.Replace(import, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        _serverConfigurationManager.SaveConfiguration("branding", branding);

        _logger.LogInformation("[VisualHome] Removed Visual Home CSS import from Jellyfin branding CSS");
        return new { Removed = true };
    }

    /// <summary>
    /// Installs the Visual Home script tags into Jellyfin Web index.html.
    /// </summary>
    /// <returns>Status payload.</returns>
    [HttpPost("install-web")]
    public ActionResult<object> InstallWebInjection()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var indexPath = GetWebIndexPath();
        if (!System.IO.File.Exists(indexPath))
        {
            return BadRequest(new { Error = "Jellyfin Web index.html was not found.", IndexPath = indexPath });
        }

        var html = System.IO.File.ReadAllText(indexPath);
        var cleaned = RemoveWebInjectionBlock(html);
        if (!cleaned.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Error = "Jellyfin Web index.html does not contain a closing body tag.", IndexPath = indexPath });
        }

        var backupPath = GetWebIndexBackupPath();
        if (!System.IO.File.Exists(backupPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
            System.IO.File.WriteAllText(backupPath, html);
        }

        var injected = cleaned.Replace("</body>", GetWebInjectionBlock() + Environment.NewLine + "</body>", StringComparison.OrdinalIgnoreCase);
        System.IO.File.WriteAllText(indexPath, injected);

        _logger.LogInformation("[VisualHome] Installed Jellyfin Web index injection at {IndexPath}", indexPath);
        return new { Installed = true, IndexPath = indexPath, BackupPath = backupPath };
    }

    /// <summary>
    /// Removes the Visual Home script tags from Jellyfin Web index.html.
    /// </summary>
    /// <returns>Status payload.</returns>
    [HttpPost("restore-web")]
    public ActionResult<object> RestoreWebInjection()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var indexPath = GetWebIndexPath();
        if (!System.IO.File.Exists(indexPath))
        {
            return BadRequest(new { Error = "Jellyfin Web index.html was not found.", IndexPath = indexPath });
        }

        var html = System.IO.File.ReadAllText(indexPath);
        System.IO.File.WriteAllText(indexPath, RemoveWebInjectionBlock(html));

        _logger.LogInformation("[VisualHome] Removed Jellyfin Web index injection at {IndexPath}", indexPath);
        return new { Removed = true, IndexPath = indexPath };
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

    private static string GetCssImport()
    {
        return "@import url('/VisualHome/assets/visualhome.css?v=0.1.0.10');";
    }

    private string GetWebIndexPath()
    {
        return Path.Combine(_serverConfigurationManager.ApplicationPaths.WebPath, "index.html");
    }

    private string GetWebIndexBackupPath()
    {
        return Path.Combine(_serverConfigurationManager.ApplicationPaths.PluginConfigurationsPath, "visualhome.index.html.bak");
    }

    private static string GetWebInjectionBlock()
    {
        return """
            <!-- VisualHome:start -->
            <script data-vh-loader="true">
            (function () {
                if (document.querySelector('script[data-vh-main]')) {
                    return;
                }

                var version = '__VISUAL_HOME_VERSION__';
                var path = location.pathname || '';
                var webIndex = path.toLowerCase().indexOf('/web');
                var basePath = webIndex >= 0 ? path.slice(0, webIndex) : '';
                var assetBase = basePath + '/VisualHome/assets/';

                if (!document.querySelector('link[data-vh-css]')) {
                    var link = document.createElement('link');
                    link.rel = 'stylesheet';
                    link.href = assetBase + 'visualhome.css?v=' + encodeURIComponent(version);
                    link.dataset.vhCss = 'true';
                    document.head.appendChild(link);
                }

                var script = document.createElement('script');
                script.src = assetBase + 'visualhome.js?v=' + encodeURIComponent(version);
                script.defer = true;
                script.dataset.vhMain = 'true';
                document.documentElement.appendChild(script);
            })();
            </script>
            <!-- VisualHome:end -->
            """.Replace("__VISUAL_HOME_VERSION__", PluginVersion, StringComparison.Ordinal);
    }

    private static string RemoveWebInjectionBlock(string html)
    {
        const string start = "<!-- VisualHome:start -->";
        const string end = "<!-- VisualHome:end -->";

        var startIndex = html.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return html;
        }

        var endIndex = html.IndexOf(end, startIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex < 0)
        {
            return html;
        }

        endIndex += end.Length;
        return html.Remove(startIndex, endIndex - startIndex).TrimEnd();
    }
}
