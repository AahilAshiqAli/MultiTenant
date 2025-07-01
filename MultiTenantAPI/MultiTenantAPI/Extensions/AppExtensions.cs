using MultiTenantAPI.Services.AccountService;
using MultiTenantAPI.Services.AdminService;
using MultiTenantAPI.Services.ContentFolder;
using MultiTenantAPI.Services.ContentHandler;
using MultiTenantAPI.Services.ContentProcessor;
using MultiTenantAPI.Services.CurrentTenant;
using MultiTenantAPI.Services.FFmpeg.Converter;
using MultiTenantAPI.Services.FFmpeg.Thumbnail;
using MultiTenantAPI.Services.FFmpeg.VideoRendition;
using MultiTenantAPI.Services.IdentityService;
using MultiTenantAPI.Services.ProgressStore;
using MultiTenantAPI.Services.RabbitMQ;

namespace MultiTenantAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            // Transients
            services.AddTransient<IContentService, ContentService>();

            // Scoped
            services.AddScoped<ICurrentTenantService, CurrentTenantService>();
            services.AddScoped<IContentProcessorService, ContentProcessorService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IAdminService, AdminService>();

            // Singleton
            services.AddSingleton<IConverterService, ConverterService>();
            services.AddSingleton<IThumbnailService, ThumbnailService>();
            services.AddSingleton<IVideoRenditionService, VideoRenditionService>();
            services.AddSingleton<IProgressStore, InMemoryProgressStore>();

            // Content Handlers
            services.AddScoped<IContentHandler, AudioContentHandler>();
            services.AddScoped<IContentHandler, VideoContentHandler>();
            services.AddScoped<IContentHandler, Mp4Handler>();
            services.AddScoped<IContentHandler, Mp3Handler>();

            // Messaging
            services.AddScoped<IRabbitMqPublisherService, RabbitMqPublisherService>();
            services.AddHostedService<RabbitMqConsumerService>();

            return services;
        }
    }

}
