using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Board.Api
{
    public class ImageStorageFunction
    {
        private readonly string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

        [FunctionName("PostImage")]
        public async Task<IActionResult> PostImage(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "PostImage/{userName}")] HttpRequest req, string userName, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function PostImage processed a request.");
            var reqString = await req.ReadAsStringAsync();
            var imageData = JsonConvert.DeserializeObject<ImageData>(reqString);
            await using var stream = new MemoryStream(imageData.ImageBytes);
            //dynamic result;
            try
            {
                var imgContainer = GetContainer(userName.ToValidContainerName());
                string fileName = $"{imageData.ImageName}.png";
                var imgBlob = imgContainer.GetBlobClient(fileName);
                await imgBlob.UploadAsync(stream);
                
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult($"Error saving file: {e.Message}\r\n{e.StackTrace}");
            }
            return new OkObjectResult($"Image {imageData.ImageName} uploaded successfully");
        }
        [FunctionName("GetAppImages")]
        public async Task<IActionResult> GetAppImages(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetAppImages")] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function GetAppImages processed a request.");
            
            var imageList = new ImageList { Category = "image", Images = new List<ImageData>() };
            var container = GetContainer();
            await foreach (var blob in container.GetBlobsAsync())
            {
                var blobName = blob.Name;
                var client = container.GetBlobClient(blobName);
                await using var stream = await client.OpenReadAsync();
                imageList.Images.Add(new ImageData { ImageName = blobName, ImageBytes = await stream.ReadFully(), CreatedOnDate = blob.Properties.CreatedOn });
            }
            log.LogInformation($"Image data retrieved for {string.Join(", ", imageList.Images.Select(x => x.ImageName))}");
            return new OkObjectResult(imageList);
        }
        [FunctionName("GetUserImages")]
        public async Task<IActionResult> GetUserImages(
               [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetUserImages/{userName}")]
            HttpRequest req, ILogger log, string userName)
        {
            log.LogInformation("C# HTTP trigger function HandImageGetStorage processed a request.");

            var imageList = new ImageList { Category = "image", Images = new List<ImageData>() };
            var container = GetContainer(userName.ToValidContainerName());
            await foreach (var blob in container.GetBlobsAsync())
            {
                var blobName = blob.Name;
                var client = container.GetBlobClient(blobName);
                await using var stream = await client.OpenReadAsync();
                imageList.Images.Add(new ImageData { ImageName = blobName, ImageBytes = await stream.ReadFully(), CreatedOnDate = blob.Properties.CreatedOn });
            }
            log.LogInformation($"Image data retrieved for {string.Join(", ", imageList.Images.Select(x => x.ImageName))}");
            return new OkObjectResult(imageList);
        }

        #region helper

        private BlobContainerClient GetContainer(string containerName = "appimages")
        {
            string blobConnectionString = connectionString;
            string blobContainerName = containerName;

            var container = new BlobContainerClient(blobConnectionString, blobContainerName);
            container.CreateIfNotExists();

            return container;
        }
      
        #endregion
    }
    public static class Helpers
    {
        public static async Task<byte[]> ReadFully(this Stream input)
        {
            await using var ms = new MemoryStream();
            await input.CopyToAsync(ms);
            return ms.ToArray();
        }
        public static string ToValidContainerName(this string str)
        {
            var sb = new StringBuilder();
            foreach (char c in str.Where(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '-'))
            {
                sb.Append(c);
            }
            return sb.ToString().ToLower();
        }
    }
}
