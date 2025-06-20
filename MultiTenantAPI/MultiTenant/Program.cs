using Amazon.S3;
using MultiTenantAPI.Controllers;
using MultiTenantAPI.Extensions;
using MultiTenantAPI.Hubs;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.ContentFolder;
using MultiTenantAPI.Services.Converter;
using MultiTenantAPI.Services.CurrentTenant;
using MultiTenantAPI.Services.ProgressStore;
using MultiTenantAPI.Services.RabbitMQ;
using Microsoft.AspNetCore.SignalR;
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
builder.Services.AddSingleton<IFFmpegService, FFmpegService>();
builder.Services.AddSingleton<IProgressStore, InMemoryProgressStore>();

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

app.MapControllers();

app.MapGroup("/api")
    .MapIdentityApi<AppUser>();
app.MapGroup("/api")
    .MapIdentityUserEndpoints()
    .MapAccountEndpoints()
    .MapAuthorizationDemoEndpoints();

app.Run();




