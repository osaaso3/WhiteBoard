using Board.Client.Models;
using Board.Client.Services;
using Board.Client.Services.Auth;
using Board.Client.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Board.Client.Pages
{
    [Authorize]
    public partial class WhiteBoardHome
    {
        [Inject]
        private IStorageClient StorageClient { get; set; }
        [Inject]
        private ICustomAuthenticationStateProvider AuthState { get; set; }
        [Inject]
        private AppState AppState { get; set; }
        private string name;
        private string imageDataUrl;
        private ImageList imagesLoaded = new();
        private string imageData;
        //private ImageData uploadedImage = new();

        private async Task OnInputFileChange(InputFileChangeEventArgs e)
        {
            imagesLoaded.Images = new List<ImageData>();
            string format = "image/png";
            //var imageFile = e.File;
            var max = 6;
            foreach (var imageFile in e.GetMultipleFiles(max))
            {
                var uploadedImage = new ImageData();
                name = imageFile.Name[0..^4];
                var resizedImageFile = await imageFile.RequestImageFileAsync(format,
                    400, 200);
                byte[] buffer = new byte[resizedImageFile.Size];

                await resizedImageFile.OpenReadStream().ReadAsync(buffer);
                imageDataUrl = $"data:{format};base64,{Convert.ToBase64String(buffer)}";
                uploadedImage.ImageBytes = buffer;
                uploadedImage.ImageName = name;
                imagesLoaded.Images.Add(uploadedImage);

            }
        }
        private async Task PostNewImage(ImageData image)
        {
            var auth = await AuthState.GetAuthenticationStateAsync();
            Console.WriteLine("Claims types:" + JsonSerializer.Serialize(auth.User.Claims.Select(x => x.Type)));
            var user = auth.User.Identity.Name;
            imageData = await StorageClient.PostNewImage(user, image);
        }
        private async Task GetUserImages()
        {
            var auth = await AuthState.GetAuthenticationStateAsync();
            Console.WriteLine("Claims types:" + JsonSerializer.Serialize(auth.User.Claims.Select(x => x.Type)));
            var user = auth.User.Identity.Name;
            imagesLoaded = await StorageClient.GetUserImage(user);
        }
    }
}
