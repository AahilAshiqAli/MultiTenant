using Microsoft.AspNetCore.Authorization;

namespace MultiTenantAPI.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder AllowAnonymousEndpoints(this IEndpointConventionBuilder endpoints)
        {
            endpoints.Add(endpointBuilder =>
            {
                var metadata = endpointBuilder.Metadata;
                if (endpointBuilder.DisplayName != null &&
                   (endpointBuilder.DisplayName.Contains("signin", StringComparison.OrdinalIgnoreCase) ||
                    endpointBuilder.DisplayName.Contains("signup", StringComparison.OrdinalIgnoreCase) ||
                    endpointBuilder.DisplayName.Contains("tenant-create", StringComparison.OrdinalIgnoreCase)))
                {
                    metadata.Add(new AllowAnonymousAttribute());
                }
            });

            return endpoints;
        }
    }

}
