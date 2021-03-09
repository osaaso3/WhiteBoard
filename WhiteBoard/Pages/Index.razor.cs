using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Board.Client.Models;
using Board.Client.Services;
using Board.Client.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Board.Client.Pages
{
    public partial class Index
    {
        [Inject]
        public ISyncLocalStorageService LocalStorage { get; set; }
        [Inject]
        private ICustomAuthenticationStateProvider AuthState { get; set; }
        [Inject]
        private AppState AppState { get; set; }
        private bool start;
        private string name;
        private string imageDataUrl;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var auth = await AuthState.GetAuthenticationStateAsync();
                var user = auth.User;
                AppState.UserName = user.Identity?.Name;
                AppState.IsAuth = user.Identity?.IsAuthenticated ?? false;
            }
        }
        private void StartWhiteboard()
        {
            start = true;
            AppState.CanvasHistory = new CanvasHistory<string>(10);
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
            AppState.CanvasHistory = LocalStorage.GetItem<CanvasHistory<string>>("CurrentHistory");
            name = lastImage.Name;
            imageDataUrl = lastImage.ImageUrl;
        }
    }
}
