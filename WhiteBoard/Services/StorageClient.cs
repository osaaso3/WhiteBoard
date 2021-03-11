using Board.Client.Models;
using Board.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Board.Client.Services
{
    public class StorageClient : IStorageClient
    {
        public HttpClient Client { get; }
        private string apiStringLocal = "http://localhost:7071/api";
        public StorageClient(HttpClient httpClient)
        {
            Client = httpClient;
        }
        public async Task<ImageList> GetUserImage(string userId)
        {
            var result = await Client.GetFromJsonAsync<ImageList>($"api/GetUserImages/{userId}");
            Console.WriteLine($"Images retrieved: {string.Join(", ", result.Images.Select(x => x.ImageName))}");
            return result;
        }
        public async Task<ImageList> GetAppImage()
        {
            var result = await Client.GetFromJsonAsync<ImageList>("api/GetAppImages");
            Console.WriteLine($"Images retrieved: {string.Join(", ", result.Images.Select(x => x.ImageName))}");
            return result;
        }
        public async Task<ImageList> GetUserTypeImages(string userId, string category)
        {
            var result = await Client.GetFromJsonAsync<ImageList>($"api/GetUserTypeImages/{userId}/{category}");
            Console.WriteLine($"Images retrieved: {string.Join(", ", result.Images.Select(x => x.ImageName))}");
            return result;
        }
        public async Task<string> PostNewImage(string userId, ImageData image)
        {
            var result = await Client.PostAsJsonAsync($"api/PostImage/{userId}", image);
            return await result.Content.ReadAsStringAsync();
        }
        
    }
}
