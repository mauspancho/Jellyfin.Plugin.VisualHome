using System.Reflection;
using System.Security.Claims;
using Jellyfin.Plugin.VisualHome.Models;
using Jellyfin.Plugin.VisualHome.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.VisualHome.Controllers;

/// <summary>
/// Read endpoints and web assets for Visual Home.
/// </summary>
[ApiController]
[Route("VisualHome")]
public sealed class VisualHomeSectionsController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, (string Resource, string ContentType)> Assets =
        new Dictionary<string, (string Resource, string ContentType)>(StringComparer.OrdinalIgnoreCase)
        {
            ["visualhome.js"] = ("Jellyfin.Plugin.VisualHome.Web.visualhome.js", "text/javascript; charset=utf-8"),
            ["visualhome.css"] = ("Jellyfin.Plugin.VisualHome.Web.visualhome.css", "text/css; charset=utf-8"),
            ["visualhome-loader.user.js"] = ("Jellyfin.Plugin.VisualHome.Web.visualhome-loader.user.js", "text/javascript; charset=utf-8")
        };

    private readonly IUserManager _userManager;
    private readonly SectionCacheService _cacheService;
    private readonly SectionQueryService _sectionQueryService;
    private readonly ILogger<VisualHomeSectionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualHomeSectionsController"/> class.
    /// </summary>
    /// <param name="userManager">User manager.</param>
    /// <param name="cacheService">Cache service.</param>
    /// <param name="sectionQueryService">Section query service.</param>
    /// <param name="logger">Logger.</param>
    public VisualHomeSectionsController(
        IUserManager userManager,
        SectionCacheService cacheService,
        SectionQueryService sectionQueryService,
        ILogger<VisualHomeSectionsController> logger)
    {
        _userManager = userManager;
        _cacheService = cacheService;
        _sectionQueryService = sectionQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets active renderable sections for the current user.
    /// </summary>
    /// <returns>Renderable sections.</returns>
    [Authorize]
    [HttpGet("sections")]
    public ActionResult<IEnumerable<VisualSectionResult>> GetActiveSections([FromQuery] string? userId = null)
    {
        var configuration = VisualHomePlugin.Instance?.Configuration;
        if (configuration is null || !configuration.PluginEnabled || !configuration.VisualInjectionEnabled)
        {
            return Array.Empty<VisualSectionResult>();
        }

        var user = GetCurrentUser(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var sections = configuration.Sections
            .Where(section => section.Enabled)
            .OrderBy(section => section.Position)
            .Select(section =>
            {
                var key = $"{user.Id:N}:{section.Id}:{section.Position}:{section.CacheMinutes}:{section.Limit}";
                return _cacheService.GetOrCreate(
                    key,
                    section.CacheMinutes,
                    () => _sectionQueryService.BuildSection(section, configuration, user));
            })
            .Where(section => section.Items.Count > 0 || !section.Success)
            .ToList();

        return sections;
    }

    /// <summary>
    /// Gets one section by id.
    /// </summary>
    /// <param name="sectionId">Section id.</param>
    /// <param name="userId">Optional current user id fallback.</param>
    /// <returns>Renderable section.</returns>
    [Authorize]
    [HttpGet("sections/{sectionId}")]
    public ActionResult<VisualSectionResult> GetSection(string sectionId, [FromQuery] string? userId = null)
    {
        var configuration = VisualHomePlugin.Instance?.Configuration;
        if (configuration is null || !configuration.PluginEnabled)
        {
            return NotFound();
        }

        var section = configuration.Sections.FirstOrDefault(s => string.Equals(s.Id, sectionId, StringComparison.OrdinalIgnoreCase));
        if (section is null || !section.Enabled)
        {
            return NotFound();
        }

        var user = GetCurrentUser(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var key = $"{user.Id:N}:{section.Id}:{section.Position}:{section.CacheMinutes}:{section.Limit}";
        return _cacheService.GetOrCreate(
            key,
            section.CacheMinutes,
            () => _sectionQueryService.BuildSection(section, configuration, user));
    }

    /// <summary>
    /// Gets public frontend configuration.
    /// </summary>
    /// <returns>Public configuration.</returns>
    [Authorize]
    [HttpGet("client-config")]
    public ActionResult<object> GetClientConfig()
    {
        var configuration = VisualHomePlugin.Instance?.Configuration;
        return new
        {
            pluginEnabled = configuration?.PluginEnabled == true,
            visualInjectionEnabled = configuration?.VisualInjectionEnabled == true,
            sidebarEnabled = configuration?.SidebarEnabled == true,
            PluginEnabled = configuration?.PluginEnabled == true,
            VisualInjectionEnabled = configuration?.VisualInjectionEnabled == true,
            SidebarEnabled = configuration?.SidebarEnabled == true
        };
    }

    /// <summary>
    /// Serves whitelisted embedded frontend assets.
    /// </summary>
    /// <param name="fileName">Asset file name.</param>
    /// <returns>Asset file.</returns>
    [AllowAnonymous]
    [HttpGet("assets/{fileName}")]
    public IActionResult GetAsset(string fileName)
    {
        if (!Assets.TryGetValue(fileName, out var asset))
        {
            return NotFound();
        }

        var assembly = typeof(VisualHomePlugin).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(asset.Resource);
        if (stream is null)
        {
            _logger.LogWarning("[VisualHome] Embedded asset {Asset} was not found", fileName);
            return NotFound();
        }

        using var reader = new StreamReader(stream);
        return Content(reader.ReadToEnd(), asset.ContentType);
    }

    private Jellyfin.Database.Implementations.Entities.User? GetCurrentUser(string? requestedUserId = null)
    {
        var userId = GetUserId(User);
        if (userId == Guid.Empty && Guid.TryParse(requestedUserId, out var queryUserId))
        {
            userId = queryUserId;
        }

        if (userId == Guid.Empty && Request.Headers.TryGetValue("X-Emby-UserId", out var embyUserId)
            && Guid.TryParse(embyUserId.ToString(), out var headerUserId))
        {
            userId = headerUserId;
        }

        if (userId == Guid.Empty && Request.Headers.TryGetValue("X-MediaBrowser-UserId", out var mediaBrowserUserId)
            && Guid.TryParse(mediaBrowserUserId.ToString(), out var mediaHeaderUserId))
        {
            userId = mediaHeaderUserId;
        }

        return userId == Guid.Empty ? null : _userManager.GetUserById(userId);
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
}
