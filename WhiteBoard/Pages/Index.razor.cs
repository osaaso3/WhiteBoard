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
        private CanvasModel canvasModel = new();

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
        private void StartWhiteboard(bool isStart)
        {
            name = canvasModel.Name;
            imageDataUrl = canvasModel.ImageUrl;
            start = true;
            AppState.CanvasHistory = new CanvasHistory<string>(10);
        }
        private void HandleUpdateCanvas(CanvasModel canvas)
        {

        }

        private void HandleNewWhiteboard(bool isNew)
        {
            start = !isNew;
            name = "";
            imageDataUrl = "";
            canvasModel = new CanvasModel();
        }
      
    }
}
