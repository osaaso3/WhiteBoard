using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board.Client.Services.Auth;
using Board.Client.Services.Interfaces;


namespace Board.Client.Services
{
    public static class AppServiceContainerExtension
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            return services.AddSingleton<AppState>()
                .AddCustomAuthentication()
                .AddScoped<WhiteboardInterop>();
        }
    }
}
