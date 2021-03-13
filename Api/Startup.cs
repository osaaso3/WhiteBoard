using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Board.Api.Startup))]
namespace Board.Api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //string connectionStringCosmos = Environment.GetEnvironmentVariable("AZURE_COSMOS_CONNECTION_STRING") ?? "";
            //builder.Services.AddSingleton(s => new CosmosClient(connectionStringCosmos));
            builder.Services.AddHttpContextAccessor();
            
        }
    }
}
