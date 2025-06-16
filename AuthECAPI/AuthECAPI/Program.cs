using Amazon.S3;
using AuthECAPI.Controllers;
using AuthECAPI.Extensions;
using AuthECAPI.Hubs;
using AuthECAPI.Models;
using AuthECAPI.Services.Converter;
using AuthECAPI.Services.CurrentTenant;
using AuthECAPI.Services.Products;
using Microsoft.AspNetCore.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();



builder.Services.AddSwaggerExplorer()
                .InjectDbContext(builder.Configuration)
                .AddAppConfig(builder.Configuration)
                .AddIdentityHandlersAndStores()
                .ConfigureIdentityOptions()
                .AddIdentityAuth(builder.Configuration)
                .AddBlobStorage(builder.Configuration);

builder.Services.AddTransient<IProductService, ProductService>();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddSingleton<IFFmpegService, FFmpegService>();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000;
});

builder.Logging.ClearProviders(); 
builder.Logging.AddConsole();     
builder.Logging.AddDebug();

Console.WriteLine("hello there");

builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WithSignalRSink(services);
});
Console.WriteLine("hello there2");
try
{
    var app = builder.Build();
    Console.WriteLine("hello there3");



    app.ConfigureSwaggerExplorer()
       .ConfigureCORS(builder.Configuration)
       .AddIdentityAuthMiddlewares();

    app.MapHub<ProgressHub>("/progressHub").RequireAuthorization();
    app.MapHub<LogHub>("/logHub").RequireAuthorization();

    app.UseStaticFiles();
    app.MapControllers();

    app.MapGroup("/api")
       .MapIdentityApi<AppUser>();
    app.MapGroup("/api")
       .MapIdentityUserEndpoints()
       .MapAccountEndpoints()
       .MapAuthorizationDemoEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("Exception during app build:");
    Console.WriteLine(ex.ToString());
    throw; // Optional: rethrow so host shuts down properly
}




