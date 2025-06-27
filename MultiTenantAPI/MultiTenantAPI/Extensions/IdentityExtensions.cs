using MultiTenantAPI.Middlewares;
using MultiTenantAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MultiTenantAPI.Extensions
{
  public static class IdentityExtensions
  {
    public static IServiceCollection AddIdentityHandlersAndStores(this IServiceCollection services)
    {
      services.AddIdentityApiEndpoints<AppUser>()
                .AddRoles<IdentityRole>()
              .AddEntityFrameworkStores<AppDbContext>();
      return services;
    }

    public static IServiceCollection ConfigureIdentityOptions(this IServiceCollection services)
    {
      services.Configure<IdentityOptions>(options =>
      {
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.User.RequireUniqueEmail = true;
      });
      return services;
    }

    //Auth = Authentication + Authorization
    public static IServiceCollection AddIdentityAuth(
        this IServiceCollection services,
        IConfiguration config)
    {
      services
      .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(y =>
        {
          y.SaveToken = false; // dont save token to httpContext bcz it is saved on client side and rest is stateless

            y.Events = new JwtBearerEvents // hoooks into token pipeline and tweak how tokens are handled
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        ( path.StartsWithSegments("/logHub")))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask; // only doing this bcz onMessageRecieved is asynchoronous delegate
                }
            };



            y.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                config["AppSettings:JWTSecret"]!)),
            ValidateIssuer = false, // who has issued this token
            ValidateAudience = false, // who is the intended audience of this token. LIke if I am using google OAuth then is the recipent me?
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
          };
        });
      services.AddAuthorization(options =>
      {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
          .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
          .RequireAuthenticatedUser() // only authenticated users can access this endpoint. Default authorize
          .Build();



      });


      return services;
    }

    public static WebApplication AddIdentityAuthMiddlewares(this WebApplication app)
    {
      app.UseAuthentication();
      app.UseMiddleware<TenantResolutionMiddleware>();
      app.UseAuthorization();
      app.UseMiddleware<SerilogTenantEnricherMiddleware>();
      return app;
    }

  }
}

