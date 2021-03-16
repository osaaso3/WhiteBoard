using Blazor.ModalDialog;
using Blazored.LocalStorage;
using Board.Client.Models;
using Board.Client.Services;
using Board.Client.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Board.Client.RazorComponents
{
    public partial class StartForm
    {
        [Inject]
        private AppState AppState { get; set; }

        [Inject]
        private ISyncLocalStorageService LocalStorage { get; set; }
        [Inject]
        private IStorageClient StorageClient { get; set; }
        [Inject]
        private IModalDialogService ModalService { get; set; }
        [Parameter]
        public EventCallback<CanvasModel> CanvasModelChanged { get; set; }
        [Parameter]
        public string DataUrl { get; set; }
        [Parameter]
        public EventCallback<bool> OnBegin { get; set; }
        [Parameter]
        public CanvasModel CanvasModel { get; set; } = new();
        private StartFormModel Form { get; set; } = new();
        class StartFormModel
        {
            public string Name { get; set; }
            [Required]
            [Range(typeof(StartOption), nameof(StartOption.New), nameof(StartOption.Retreive),
                ErrorMessage = "Select a start option")]
            public StartOption StartOption { get; set; } = StartOption.None;
        }
        enum StartOption
        {
            [Description("Start new")]
            New,
            [Description("From uploaded whiteboard image")]
            Upload,
            [Description("From local (browser) storage")]
            Local,
            [Description("From cloud storage")]
            Cloud,
            [Description("Retreive last")]
            Retreive,
            [Description("Choose...")]
            None
        }
        private bool isUpload;
        //private bool disabled => Form.StartOption == StartOption.Cloud || Form.StartOption == StartOption.Local;
        private string buttonLabel => Form.StartOption != StartOption.New ? "Select whiteboard" : "Start";
        private bool isNameMissing;
        private async Task OnInputFileChange(InputFileChangeEventArgs e)
        {
            string format = "image/png";
            var imageFile = e.File;

            //name = imageFile.Name[0..^4];
            var resizedImageFile = await imageFile.RequestImageFileAsync(format,
                1200, 600);
            byte[] buffer = new byte[resizedImageFile.Size];
            await resizedImageFile.OpenReadStream().ReadAsync(buffer);
            Form.Name ??= imageFile.Name[0..^4];
            DataUrl = $"data:{format};base64,{Convert.ToBase64String(buffer)}";
            CanvasModel = new CanvasModel { Name = Form.Name, ImageUrl = DataUrl };
            await CanvasModelChanged.InvokeAsync(CanvasModel);

        }
        private async Task SubmitForm()
        {
            if (Form.StartOption == StartOption.Cloud || Form.StartOption == StartOption.Local)
            {
                var images = Form.StartOption == StartOption.Cloud ? await GetFromCloud() : await GetFromLocal();
                var imageList = new ImageList { Images = images };
                var parameters = new ModalDialogParameters { { "ImageList", imageList } };
                var modalResult = await ModalService.ShowDialogAsync<ImageMenuModal>("Select Whiteboard Image", parameters: parameters);
                if (!modalResult.Success) return;
                var selectedImage = modalResult.ReturnParameters.Get<ImageData>("SelectedImage");
                Form.Name = selectedImage.ImageName;
                DataUrl = selectedImage.ImageBytes?.ToImageUrl();
                CanvasModel = new CanvasModel { Name = Form.Name, ImageUrl = DataUrl };
                await CanvasModelChanged.InvokeAsync(CanvasModel);
            }
            else if (Form.StartOption == StartOption.Upload) { isUpload = true; }
            else if (Form.StartOption == StartOption.Retreive) { GetRecover(); }
            else if (Form.StartOption == StartOption.New)
            {
                if (string.IsNullOrEmpty(Form.Name))
                {
                    isNameMissing = true;
                    await ModalService.ShowMessageBoxAsync("Name missing", "You forgot to name the new whiteboard, dummy!");
                    return;
                }
                await Start();
            }
            isNameMissing = false;
        }
        private async Task<List<ImageData>> GetFromCloud()
        {
            var imageList = await StorageClient.GetUserTypeImages(AppState.UserName, "Whiteboard");
            return imageList.Images;
        }
        private Task<List<ImageData>> GetFromLocal()
        {
            var imageList = new List<ImageData>();
            
            for (int i = 0; i < LocalStorage.Length(); i++)
            {
                Console.WriteLine($"key {i+1} in local storage: {LocalStorage.Key(i)}");
                if (LocalStorage.Key(i).Contains($"{AppState.UserName}-Whiteboard"))
                {
                    imageList.Add(LocalStorage.GetItem<ImageData>(LocalStorage.Key(i)));
                }
            }
            return Task.FromResult(imageList);
        }
        //private void NameTrigger(StartOption option)
        //{
        //    showName = option == StartOption.New || option == StartOption.Upload || option == StartOption.Retreive;
        //    buttonLabel = option != StartOption.New ? "Select whiteboard" : "Start";
        //    StateHasChanged();
        //}
        private void GetRecover()
        {
            var lastImage = LocalStorage.GetItem<CanvasModel>("LastCanvas");
            DataUrl = lastImage.ImageUrl;
            Form.Name ??= lastImage.Name;
            CanvasModel = new CanvasModel { Name = Form.Name, ImageUrl = DataUrl };
            StateHasChanged();
        }
        private async Task Start()
        {
            if (string.IsNullOrEmpty(Form.Name))
            {
                isNameMissing = true;
                await ModalService.ShowMessageBoxAsync("Name missing", "You forgot to name the new whiteboard, dummy!");
                return;
            }
            CanvasModel = new CanvasModel { Name = Form.Name, ImageUrl = DataUrl };
            await CanvasModelChanged.InvokeAsync(CanvasModel);
            await OnBegin.InvokeAsync(true);
        }
    }
}
