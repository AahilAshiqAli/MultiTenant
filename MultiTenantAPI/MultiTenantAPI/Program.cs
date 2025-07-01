using Amazon.S3;
using Microsoft.AspNetCore.SignalR;
using MultiTenantAPI.Controllers;
using MultiTenantAPI.Extensions;
using MultiTenantAPI.Hubs;
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


builder.Services.AddAppServices();
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


builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

var app = builder.Build();


app.UseStaticFiles();


app.ConfigureSwaggerExplorer()
    .ConfigureCORS(builder.Configuration)
    .AddIdentityAuthMiddlewares();

app.MapHub<LogHub>("/logHub").RequireAuthorization();

app.MapControllers().AllowAnonymousEndpoints();


app.Run();




