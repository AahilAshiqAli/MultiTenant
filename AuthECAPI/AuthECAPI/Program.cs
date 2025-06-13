using Amazon.S3;
using AuthECAPI.Controllers;
using AuthECAPI.Extensions;
using AuthECAPI.Hubs;
using AuthECAPI.Models;
using AuthECAPI.Services.Converter;
using AuthECAPI.Services.CurrentTenant;
using AuthECAPI.Services.Products;
using AuthECAPI.Services.SignalR;
using Microsoft.AspNetCore.SignalR;


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

builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();


app.MapHub<ProgressHub>("/progressHub");


app.ConfigureSwaggerExplorer()
   .ConfigureCORS(builder.Configuration)
   .AddIdentityAuthMiddlewares();

app.UseStaticFiles();
app.MapControllers();

app.MapGroup("/api")
   .MapIdentityApi<AppUser>();
app.MapGroup("/api")
   .MapIdentityUserEndpoints()
   .MapAccountEndpoints()
   .MapAuthorizationDemoEndpoints();

app.Run();



