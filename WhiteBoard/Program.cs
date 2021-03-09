using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazor.ModalDialog;
using Blazored.LocalStorage;
using Board.Client.Services;
using Board.Client.Services.Interfaces;
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
            var baseApiAddress = builder.Configuration["BaseAddress"] ?? builder.HostEnvironment.BaseAddress;
            var clientAddress = builder.Configuration["ClientAddress"] ?? builder.HostEnvironment.BaseAddress;
            builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(baseApiAddress) });
            builder.Services.AddHttpClient<IStorageClient, StorageClient>(clnt => clnt.BaseAddress = new Uri(baseApiAddress));
            builder.Services.AddAppServices();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddModalDialog();
            await builder.Build().RunAsync();
        }
    }
}
