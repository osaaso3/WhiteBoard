using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Board.Client.Services.Auth
{
    public static class AuthServiceExtension
    {
        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services)
        {
            return services
                .AddAuthorizationCore()
                .AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>()
                .AddScoped<ICustomAuthenticationStateProvider, CustomAuthenticationStateProvider>();
        }
    }
}