using CodeKids.Application.Abstractions;
using CodeKids.Infrastructure.WhatsApp;
using CodeKids.Infrastructure.Zoom;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeKids.Infrastructure;

public static class IntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddZoomIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZoomOptions>(configuration.GetSection(ZoomOptions.SectionName));
        services.AddHttpClient(nameof(ZoomMeetingClient), client =>
        {
            client.BaseAddress = new Uri("https://api.zoom.us/v2/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
        services.AddHttpClient(nameof(ZoomUserOAuthService));
        services.AddSingleton<IZoomMeetingClient, ZoomMeetingClient>();
        services.AddSingleton<IZoomUserOAuthService, ZoomUserOAuthService>();
        return services;
    }

    public static IServiceCollection AddWhatsAppIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        services.AddHttpClient(nameof(WhatsAppClient), client =>
        {
            client.BaseAddress = new Uri("https://graph.facebook.com/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
        services.AddSingleton<IWhatsAppClient, WhatsAppClient>();
        return services;
    }
}
