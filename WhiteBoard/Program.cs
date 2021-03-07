using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazor.ModalDialog;
using Blazored.LocalStorage;
using Board.Client.Services;
using Board.Client.Services.Auth;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Board.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            var baseAddress = builder.Configuration["BaseAddress"] ?? builder.HostEnvironment.BaseAddress;
            var clientAddress = builder.Configuration["ClientAddress"] ?? builder.HostEnvironment.BaseAddress;
            builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(baseAddress) });
            //builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });
            builder.Services.AddHttpClient<StorageClient>(clnt => clnt.BaseAddress = new Uri(baseAddress));
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddScoped<WhiteboardInterop>();
            builder.Services.AddCustomAuthentication();
            builder.Services.AddModalDialog();
            await builder.Build().RunAsync();
        }
    }
}
