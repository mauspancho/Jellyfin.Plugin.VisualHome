using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.VisualHome.Services;

/// <summary>
/// Adds a non-destructive response middleware that injects Visual Home assets into Jellyfin Web.
/// </summary>
public sealed class VisualHomeStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<VisualHomeInjectionMiddleware>();
            next(app);
        };
    }
}

/// <summary>
/// Injects Visual Home script tags into Jellyfin Web HTML responses without editing web files on disk.
/// </summary>
public sealed class VisualHomeInjectionMiddleware
{
    private const string ScriptMarker = "data-vh-main";
    private readonly RequestDelegate _next;
    private readonly ILogger<VisualHomeInjectionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualHomeInjectionMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next middleware.</param>
    /// <param name="logger">Logger.</param>
    public VisualHomeInjectionMiddleware(RequestDelegate next, ILogger<VisualHomeInjectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Invoke(HttpContext context)
    {
        if (!ShouldInspectRequest(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var originalBody = context.Response.Body;
        var originalAcceptEncoding = context.Request.Headers.AcceptEncoding.ToString();
        context.Request.Headers.AcceptEncoding = string.Empty;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context).ConfigureAwait(false);

            buffer.Position = 0;
            if (!ShouldInjectResponse(context))
            {
                await buffer.CopyToAsync(originalBody).ConfigureAwait(false);
                return;
            }

            using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var html = await reader.ReadToEndAsync().ConfigureAwait(false);
            var injected = InjectAssets(html, context.Request.PathBase);
            var bytes = Encoding.UTF8.GetBytes(injected);

            context.Response.Body = originalBody;
            context.Response.ContentLength = bytes.Length;
            context.Response.Headers.Remove("ETag");
            context.Response.Headers.Remove("Content-MD5");
            await context.Response.Body.WriteAsync(bytes).ConfigureAwait(false);

            if (!ReferenceEquals(html, injected))
            {
                _logger.LogInformation("[VisualHome] Injected frontend assets into Jellyfin Web HTML");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VisualHome] HTML injection failed");
            context.Response.Body = originalBody;
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody).ConfigureAwait(false);
        }
        finally
        {
            context.Request.Headers.AcceptEncoding = originalAcceptEncoding;
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldInspectRequest(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        return path.EndsWith("/web", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/web/", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/web/index.html", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldInjectResponse(HttpContext context)
    {
        var configuration = VisualHomePlugin.Instance?.Configuration;
        if (configuration is null || !configuration.PluginEnabled || !configuration.VisualInjectionEnabled)
        {
            return false;
        }

        return context.Response.StatusCode == StatusCodes.Status200OK
            && (context.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string InjectAssets(string html, PathString pathBase)
    {
        if (html.Contains(ScriptMarker, StringComparison.OrdinalIgnoreCase)
            || !html.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            return html;
        }

        var basePath = pathBase.HasValue ? pathBase.Value : string.Empty;
        var cssUrl = $"{basePath}/VisualHome/assets/visualhome.css?v=0.1.0.9";
        var jsUrl = $"{basePath}/VisualHome/assets/visualhome.js?v=0.1.0.9";
        var tags = $"""
            <link rel="stylesheet" href="{cssUrl}" data-vh-css="true">
            <script src="{jsUrl}" defer data-vh-main="true"></script>
            """;

        return html.Replace("</body>", tags + Environment.NewLine + "</body>", StringComparison.OrdinalIgnoreCase);
    }
}
