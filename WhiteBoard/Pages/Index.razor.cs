using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Board.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Board.Client.Pages
{
    public partial class Index
    {
        [Inject]
        public ISyncLocalStorageService LocalStorage { get; set; }
        private bool start;
        private string name;
        private string imageDataUrl;

        private void StartWhiteboard()
        {

        }

        private void HandleNewWhiteboard(bool isNew)
        {
            start = !isNew;
            name = "";
            imageDataUrl = "";
        }
        private async Task OnInputFileChange(InputFileChangeEventArgs e)
        {
            string format = "image/png";
            var imageFile = e.File;

            name = imageFile.Name.Substring(0, imageFile.Name.Length - 4);
            var resizedImageFile = await imageFile.RequestImageFileAsync(format,
                1200, 600);
            byte[] buffer = new byte[resizedImageFile.Size];
            await resizedImageFile.OpenReadStream().ReadAsync(buffer);
            imageDataUrl = $"data:{format};base64,{Convert.ToBase64String(buffer)}";


        }

        private void RecoverLast()
        {
            var lastImage = LocalStorage.GetItem<CanvasModel>("LastCanvas");
            name = lastImage.Name;
            imageDataUrl = lastImage.ImageUrl;
        }
    }
}
