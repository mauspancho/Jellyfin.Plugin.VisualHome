using Jellyfin.Plugin.VisualHome.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.VisualHome;

/// <summary>
/// Registers Visual Home services in Jellyfin DI.
/// </summary>
public sealed class VisualHomeServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<SectionCacheService>();
        serviceCollection.AddSingleton<RecommendationService>();
        serviceCollection.AddSingleton<SectionQueryService>();
        serviceCollection.AddSingleton<IStartupFilter, VisualHomeStartupFilter>();
    }
}
