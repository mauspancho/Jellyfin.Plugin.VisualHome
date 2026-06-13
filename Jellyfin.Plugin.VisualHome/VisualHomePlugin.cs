using System.Globalization;
using Jellyfin.Plugin.VisualHome.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.VisualHome;

/// <summary>
/// Jellyfin Visual Home plugin entry point.
/// </summary>
public sealed class VisualHomePlugin : BasePlugin<VisualHomeConfiguration>, IHasWebPages
{
    /// <summary>
    /// Plugin id.
    /// </summary>
    public static readonly Guid PluginId = Guid.Parse("3f09ff47-54d2-4d7a-bf43-cf54d17e6701");

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualHomePlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public VisualHomePlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Jellyfin Visual Home";

    /// <inheritdoc />
    public override Guid Id => PluginId;

    /// <summary>
    /// Gets the active plugin instance.
    /// </summary>
    public static VisualHomePlugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.VisualHomePluginConfigurationPage.html",
                    GetType().Namespace)
            }
        ];
    }
}
