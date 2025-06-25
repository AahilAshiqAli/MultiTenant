using Amazon.S3;
using Microsoft.AspNetCore.SignalR;
using MultiTenantAPI.Controllers;
using MultiTenantAPI.Extensions;
using MultiTenantAPI.Hubs;
using MultiTenantAPI.Services.AccountService;
using MultiTenantAPI.Services.AdminService;
using MultiTenantAPI.Services.ContentFolder;
using MultiTenantAPI.Services.ContentProcessor;
using MultiTenantAPI.Services.FFmpeg.Converter;
using MultiTenantAPI.Services.FFmpeg.Thumbnail;
using MultiTenantAPI.Services.FFmpeg.VideoRendition;
using MultiTenantAPI.Services.CurrentTenant;
using MultiTenantAPI.Services.IdentityService;
using MultiTenantAPI.Services.ProgressStore;
using MultiTenantAPI.Services.RabbitMQ;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();


builder.Services.AddSwaggerExplorer()
                .InjectDbContext(builder.Configuration)
                .AddAppConfig(builder.Configuration)
                .AddIdentityHandlersAndStores()
                .ConfigureIdentityOptions()
                .AddIdentityAuth(builder.Configuration)
                .AddBlobStorage(builder.Configuration);

builder.Services.AddTransient<IContentService, ContentService>();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddSingleton<IConverterService, ConverterService>();
builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();
builder.Services.AddSingleton<IVideoRenditionService, VideoRenditionService>();
builder.Services.AddScoped<IContentProcessorService, ContentProcessorService>();
builder.Services.AddSingleton<IProgressStore, InMemoryProgressStore>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000;
});

builder.Logging.ClearProviders(); 
builder.Logging.AddConsole();     
builder.Logging.AddDebug();



builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WithSignalRSink(services);
});

builder.Services.AddScoped<IRabbitMqPublisherService, RabbitMqPublisherService>();
builder.Services.AddHostedService<RabbitMqConsumerService>();

var app = builder.Build();


app.UseStaticFiles();


app.ConfigureSwaggerExplorer()
    .ConfigureCORS(builder.Configuration)
    .AddIdentityAuthMiddlewares();

app.MapHub<LogHub>("/logHub").RequireAuthorization();

app.MapControllers().AllowAnonymousEndpoints();


app.Run();




