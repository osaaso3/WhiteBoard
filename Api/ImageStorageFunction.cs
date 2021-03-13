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
using Microsoft.Azure.Cosmos;
using System.Net.Http;
using Microsoft.Azure.Cosmos.Linq;
using Azure.Storage.Blobs.Models;

namespace Board.Api
{
    public class ImageStorageFunction
    {
        private readonly string connectionStringBlob = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        private readonly string connectionStringCosmos = Environment.GetEnvironmentVariable("AZURE_COSMOS_CONNECTION_STRING") ?? "";
        private readonly CosmosClient CosmosClient;
        public ImageStorageFunction()
        {
            CosmosClient = new CosmosClient(connectionStringCosmos);
        }

        [FunctionName("PostImage")]
        public async Task<IActionResult> PostImage(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "PostImage/{userName}")] HttpRequest req, string userName, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function PostImage processed a request.");
            var reqString = await req.ReadAsStringAsync();
            log.LogInformation($"imageData:\r\n{reqString.Substring(0, 200)}");
            var imageData = JsonConvert.DeserializeObject<ImageData>(reqString);
            await using var stream = new MemoryStream(imageData.ImageBytes);
            //dynamic result;
            try
            {
                var imgContainer = GetContainer(userName.ToValidContainerName());
                string fileName = $"{imageData.ImageName}.png";
                var imgBlob = imgContainer.GetBlobClient(fileName);
                await imgBlob.UploadAsync(stream, overwrite: true);
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult($"Error saving file: {e.Message}\r\n{e.StackTrace}");
            }

            return new OkObjectResult($"Image {imageData.ImageName} uploaded successfully");
        }
        [FunctionName("SaveImage")]
        public async Task<IActionResult> SaveImage(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "SaveImage/{userName}")] HttpRequest req, string userName, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function SaveImage processed a request.");
            var reqString = await req.ReadAsStringAsync();

            try
            {
                var imageData = JsonConvert.DeserializeObject<ImageData>(reqString);
                await using var stream = new MemoryStream(imageData.ImageBytes);

                var cosmosContainer = CosmosClient.GetContainer("WhiteboardDb", "Images");
                imageData.Id ??= $"{imageData.UserName}-{imageData.Category}-{imageData.ImageName}";
                imageData.ImageBytes = new byte[0];
                imageData.UserName ??= userName;
                log.LogInformation(JsonConvert.SerializeObject(imageData, Formatting.Indented));
                var result = await cosmosContainer.UpsertItemAsync(imageData);
                log.LogInformation($"Results:\r\nStatus code: {result.StatusCode}\r\nContent: {result.Resource}");
                return new OkObjectResult($"Image {imageData.ImageName} saved successfully");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
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
            try
            {
                var cosmosContainer = CosmosClient.GetContainer("WhiteboardDb", "Images");
                var imageQuery = new List<ImageData>();
                using (var imageIterator = cosmosContainer.GetItemLinqQueryable<ImageData>().ToFeedIterator())
                {
                    while (imageIterator.HasMoreResults)
                    {
                        var resultSet = await imageIterator.ReadNextAsync();
                        imageQuery.AddRange(resultSet.Where(x => x.UserName == userName));
                    }
                }
                var imageList = new ImageList { Category = "general", Images = new List<ImageData>() };
                var container = GetContainer(userName.ToValidContainerName());
                await foreach (var blob in container.GetBlobsAsync())
                {
                    var blobName = blob.Name;
                    var imageMatch = imageQuery.Find(x => x.ImageName == blobName.NoFileExt());
                    if (imageMatch == null) continue;
                    var client = container.GetBlobClient(blobName);
                    await using var stream = await client.OpenReadAsync();
                    imageMatch.CreatedOnDate = blob.Properties.CreatedOn;
                    imageMatch.ImageBytes = await stream.ReadFully();
                    imageList.Images.Add(imageMatch);
                }
                log.LogInformation($"Image data retrieved for {string.Join(", ", imageList.Images.Select(x => x.ImageName))}");
                return new OkObjectResult(imageList);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\r\n{ex.StackTrace}");
                return new BadRequestObjectResult($"{ex.Message}\r\n{ex.StackTrace}");
            }

        }
        [FunctionName("GetUserTypeImages")]
        public async Task<IActionResult> GetUserTypeImages(
               [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetUserTypeImages/{userName}/{category}")]
            HttpRequest req, ILogger log, string userName, string category)
        {
            log.LogInformation("C# HTTP trigger function HandImageGetStorage processed a request.");
            var imageQuery = new List<ImageData>();
            var cosmosContainer = CosmosClient.GetContainer("WhiteboardDb", "Images");
            using (var imageIterator = cosmosContainer.GetItemLinqQueryable<ImageData>().ToFeedIterator())
            {
                while (imageIterator.HasMoreResults)
                {
                    var resultSet = await imageIterator.ReadNextAsync();
                    imageQuery.AddRange(resultSet.Where(x => x.UserName == userName && x.Category == category));
                }
            }
            var imageList = new ImageList { Category = category, Images = new List<ImageData>() };

            var container = GetContainer(userName.ToValidContainerName());
            await foreach (var blob in container.GetBlobsAsync())
            {
                var blobName = blob.Name;
                var imageMatch = imageQuery.Find(x => x.ImageName == blobName.NoFileExt());
                if (imageMatch == null) continue;
                var client = container.GetBlobClient(blobName);
                await using var stream = await client.OpenReadAsync();
                imageMatch.CreatedOnDate = blob.Properties.CreatedOn;
                imageMatch.ImageBytes = await stream.ReadFully();
                imageList.Images.Add(imageMatch);
            }
            log.LogInformation($"Image data retrieved for {string.Join(", ", imageList.Images.Select(x => x.ImageName))}");
            return new OkObjectResult(imageList);
        }
        #region helper

        private BlobContainerClient GetContainer(string containerName = "appimages")
        {
            string blobConnectionString = connectionStringBlob;
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
        public static string NoFileExt(this string file)
        {
            return file.Substring(0, file.LastIndexOf('.'));
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
