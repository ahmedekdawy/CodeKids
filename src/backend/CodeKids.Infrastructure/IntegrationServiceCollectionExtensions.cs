using CodeKids.Application.Abstractions;
using CodeKids.Application.Options;
using CodeKids.Infrastructure.Ai;
using CodeKids.Infrastructure.Email;
using CodeKids.Infrastructure.Jobs;
using CodeKids.Infrastructure.Media;
using CodeKids.Infrastructure.WhatsApp;
using CodeKids.Infrastructure.Zoom;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeKids.Infrastructure;

public static class IntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddMediaStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IMediaAccessTokenService, MediaAccessTokenService>();
        services.AddHostedService<DailyReportHostedService>();
        return services;
    }

    public static IServiceCollection AddEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IEmailService, EmailService>();
        return services;
    }

    public static IServiceCollection AddZoomIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZoomOptions>(configuration.GetSection(ZoomOptions.SectionName));
        services.AddHttpClient(nameof(ZoomMeetingClient), client =>
        {
            client.BaseAddress = new Uri("https://api.zoom.us/v2/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
        services.AddHttpClient(nameof(ZoomUserOAuthService));
        services.AddSingleton<IZoomOAuthSettingsStore, ZoomOAuthSettingsStore>();
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

    public static IServiceCollection AddStudyPlanAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddHttpClient(nameof(StudyPlanAiClient), client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CodeKids/1.0");
        });
        services.AddSingleton<IStudyPlanAiClient, StudyPlanAiClient>();
        return services;
    }
}
