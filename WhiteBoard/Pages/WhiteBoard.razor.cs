using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Blazor.ModalDialog;
using Blazored.LocalStorage;
using Board.Client.Models;
using Board.Client.Services;
using Board.Client.Services.Interfaces;
using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Board.Client.Pages
{
    public partial class WhiteBoard : IDisposable
    {
        [Inject]
        private IJSRuntime Js { get; set; }
        [Inject]
        private WhiteboardInterop Interop { get; set; }
        [Inject]
        private ISyncLocalStorageService LocalStorage { get; set; }
        [Inject]
        private AppState AppState { get; set; }
        [Inject]
        private IModalDialogService ModalService { get; set; }
        [Inject]
        private IStorageClient StorageClient { get; set; }
        private ElementReference _container;
        private Canvas _canvas;
        private Context2D _context2D;
        private Location _canvasLoc = new();
        private Location _oldMouseLocation = new();
        private Location _mouseLocation = new();
        private Location _touchLocation = new();
        private Location _oldTouchLocation = new();
        private Specs _canvasSpecs = new(600, 1200);
        private StickyNote _currentNote = new();
        private bool _isMouseDown;
        private string _color = "black";
        private bool _shouldRender = true;
        private double _lineWidth = 3;
        private bool _isEraseMode;
        private string _selectedOption;
        private string _text;
        private bool _isTouch;
        private bool _isLineMode;
        private string _tempDataUrl;
        private bool _isRecord;
        private string _description;


        private string Pointer => _isEraseMode ? "erase-mode" : "marker-mode";
        [Parameter]
        public string Name { get; set; } = "new";
        [Parameter]
        public EventCallback<bool> IsStartNew { get; set; }
        [Parameter]
        public string DataUrl { get; set; }

        protected override Task OnInitializedAsync()
        {
            AppState.PropertyChanged += UpdateState;
            return base.OnInitializedAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var containerLocation = await Interop.GetContainerLocation(_container.Id);
                (_canvasLoc.X, _canvasLoc.Y) = (containerLocation.X, containerLocation.Y);
                var specs = await Interop.GetCanvasSize(_container.Id);
                _canvasSpecs = new Specs(specs.H, specs.W);
                await InvokeAsync(StateHasChanged);

                _context2D = await _canvas.GetContext2DAsync();
                if (!string.IsNullOrWhiteSpace(DataUrl))
                    await _context2D.DrawImageAsync("image", 0, 0);

                await InitCanvasSettings();

            }
            //Console.WriteLine($"color: {_color} line: {_lineWidth} should render: {_shouldRender}");
        }

        private async Task InitCanvasSettings()
        {
            await _context2D.GlobalCompositeOperationAsync(CompositeOperation.Source_Over);
            await _context2D.StrokeStyleAsync(_color);
            await _context2D.LineWidthAsync(_lineWidth);
            await _context2D.LineJoinAsync(LineJoin.Round);
            await _context2D.LineCapAsync(LineCap.Round);
        }

        private async Task HandleChangeColor(string color)
        {
            //string color = AppState.Color;
            _color = string.IsNullOrWhiteSpace(color) ? "black" : color;
            await _context2D.StrokeStyleAsync(_color);
        }

        private async Task HandleSave()
        {
            var (cloud, local, download) = await DisplaySaveOptions();
            if (!cloud && !local && !download) return;
            var imageUrl = await _canvas.ToDataURLAsync();
            var imageBytes = Convert.FromBase64String(imageUrl.Split(',')[1]);
            var imageData = new ImageData
            {
                ImageName = Name,
                UserName = AppState.UserName,
                Category = "Whiteboard",
                ImageBytes = imageBytes,
                Id = $"{AppState.UserName}-Whiteboard-{Name}",
                Description = _description
            };
            if (cloud) await StorageClient.PostNewImage(AppState.UserName, imageData);
            if (download) await Interop.DownloadProjectFile($"{Name}.png", imageBytes);
            if (local) LocalStorage.SetItem($"{imageData.Id}", imageData);

        }
        private async Task<(bool, bool, bool)> DisplaySaveOptions()
        {
            var toCloud = false;
            var toLocal = true;
            var toDownload = false;
            ModalDataInputForm frm = new ModalDataInputForm("Save Canvas", "Select save options");
            var descFld = frm.AddStringField("Description", "Whiteboard Description", "", "Private a brief description");
            var toCloudFld = AppState.IsAuth ? frm.AddBoolField("Cloud", "To Cloud Storage", toCloud, "save to cloud storage?") : null;
            var toLocalFld = frm.AddBoolField("Local", "To Local (Browser) Storage", toLocal, "save to local (browser) storage?");
            var toDownloadFld = frm.AddBoolField("Download", "Download image", toDownload, "Download whiteboard image?");
            await frm.ShowAsync(ModalService);
            toCloud = toCloudFld?.Value ?? false;
            toLocal = toLocalFld.Value;
            toDownload = toDownloadFld.Value;
            _description = descFld.Value;
            return (toCloud, toLocal, toDownload);

        }
        private async Task HandleStartNew()
        {
            await SaveCanvasStateToLocalStorage();
            AppState.CanvasHistory.Clear();
            await IsStartNew.InvokeAsync(true);
        }

        private async Task HandleToggleEraseMode(bool isErase)
        {
            _isEraseMode = isErase;
            _color = isErase ? "white" : "black";
            _lineWidth = isErase ? 20 : 3;
            await InvokeAsync(StateHasChanged);
            _shouldRender = false;
            await _context2D.StrokeStyleAsync(_color);
            await _context2D.LineWidthAsync(_lineWidth);
        }

        private async Task HandleWidthChange(double width)
        {
            _lineWidth = width;
            await _context2D.LineWidthAsync(_lineWidth);
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleClearAndResize()
        {

            var modalConfirm = await ModalService.ShowMessageBoxAsync("Confirm Delete", "Are you sure you want clear the whiteboard?", MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2);

            if (modalConfirm == MessageBoxDialogResult.No) return;
            await SaveCanvasStateToLocalStorage();
            await _context2D.ClearRectAsync(0, 0, _canvasSpecs.W, _canvasSpecs.H);
            var specs = await Interop.GetCanvasSize(_container.Id);
            _canvasSpecs = new Specs(specs.H, specs.W);
            AppState.CanvasHistory.Clear();
            await InvokeAsync(StateHasChanged);

        }

        private async Task SaveCanvasStateToLocalStorage()
        {
            var imageUrl = await _canvas.ToDataURLAsync();
            var canvasData = new CanvasModel
            { Name = Name, ImageUrl = imageUrl, MarkerWidth = _lineWidth, Color = _color };

            LocalStorage.SetItem("LastCanvas", canvasData);
        }
        private void SaveToHistory(string imageUrl)
        {
            Console.WriteLine("Add to stack");
            AppState.CanvasHistory.Insert(imageUrl);
        }

        private async Task DoubleClickCanvas(MouseEventArgs e)
        {
            _mouseLocation.X = e.ClientX - _canvasLoc.X;
            _mouseLocation.Y = e.ClientY - _canvasLoc.Y;
            if (_selectedOption == "Text")
            {
                await _context2D.FontAsync($"{10 * _lineWidth}px serif");
                await _context2D.StrokeTextAsync(_text, _mouseLocation.X, _mouseLocation.Y);
            }
            else if (_selectedOption == "Rectangle")
            {
                await _context2D.StrokeRectAsync(_mouseLocation.X, _mouseLocation.Y, 50 * _lineWidth, 40 * _lineWidth);
            }
            else if (_selectedOption == "Oval")
            {
                await _context2D.BeginPathAsync();
                await _context2D.EllipseAsync(_mouseLocation.X, _mouseLocation.Y, 25 * _lineWidth, 20 * _lineWidth, 0, 0, 360);
                await _context2D.StrokeAsync();
            }
            else if (_selectedOption == "Sticky")
            {
                await _context2D.DrawImageAsync("stickyNote", _mouseLocation.X, _mouseLocation.Y);
            }
            else if (_selectedOption == "Image")
            {
                await _context2D.DrawImageAsync("userImage", _mouseLocation.X, _mouseLocation.Y);
            }
            _tempDataUrl = await _canvas.ToDataURLAsync();
            SaveToHistory(_tempDataUrl);
        }
        private async Task MouseDownCanvas(MouseEventArgs e)
        {
            _shouldRender = false;
            _oldMouseLocation.X = _mouseLocation.X = e.ClientX - _canvasLoc.X;
            _oldMouseLocation.Y = _mouseLocation.Y = e.ClientY - _canvasLoc.Y;
            _isMouseDown = true;
            if (!_isLineMode) return;
            _tempDataUrl = await _canvas.ToDataURLAsync();

            //SaveToHistory(_tempDataUrl);
        }

        private async Task MouseUpCanvas(MouseEventArgs e)
        {
            _shouldRender = false;
            if (_isRecord && _isMouseDown)
            {
                _tempDataUrl = await _canvas.ToDataURLAsync();
                SaveToHistory(_tempDataUrl);
                _isRecord = false;
            }
            _isMouseDown = false;

        }

        private async Task MouseMoveCanvasAsync(MouseEventArgs e)
        {
            _shouldRender = false;
            if (!_isMouseDown) return;

            _mouseLocation.X = e.ClientX - _canvasLoc.X;
            _mouseLocation.Y = e.ClientY - _canvasLoc.Y;
            if (_isLineMode)
            {
                await _context2D.ClearRectAsync(0, 0, _canvasSpecs.W, _canvasSpecs.H);
                await _context2D.DrawImageAsync("tempImage", 0, 0);
            }
            _isRecord = true;
            await DrawCanvasAsync(_mouseLocation.X, _mouseLocation.Y, _oldMouseLocation.X, _oldMouseLocation.Y);
            if (_isLineMode) return;
            _oldMouseLocation.X = _mouseLocation.X;
            _oldMouseLocation.Y = _mouseLocation.Y;
        }
        private async Task TouchStart(TouchEventArgs e)
        {
            _shouldRender = false;
            var touch = e.TargetTouches[0];
            _oldTouchLocation.X = _touchLocation.X = touch.ClientX - _canvasLoc.X;
            _oldTouchLocation.Y = _touchLocation.Y = touch.ClientY - _canvasLoc.Y;
            _isTouch = true;
            if (!_isLineMode) return;
            _tempDataUrl = await _canvas.ToDataURLAsync();

        }
        private async Task TouchMoveAsync(TouchEventArgs e)
        {
            _shouldRender = false;
            if (!_isTouch) return;

            var touch = e.TargetTouches[0];
            _touchLocation.X = touch.ClientX - _canvasLoc.X;
            _touchLocation.Y = touch.ClientY - _canvasLoc.Y;
            if (_isLineMode)
            {
                await _context2D.ClearRectAsync(0, 0, _canvasSpecs.W, _canvasSpecs.H);
                await _context2D.DrawImageAsync("tempImage", 0, 0);
            }
            _isRecord = true;
            await DrawCanvasAsync(_touchLocation.X, _touchLocation.Y, _oldTouchLocation.X, _oldTouchLocation.Y);
            if (_isLineMode) return;
            _oldTouchLocation.X = _touchLocation.X;
            _oldTouchLocation.Y = _touchLocation.Y;
        }
        private async Task TouchEnd(TouchEventArgs e)
        {
            _shouldRender = false;
            if (_isRecord && _isTouch)
            {
                _tempDataUrl = await _canvas.ToDataURLAsync();
                SaveToHistory(_tempDataUrl);
                _isRecord = false;
            }
            _isTouch = false;
        }
        private async Task UndoAsync()
        {
            //SaveToHistory(await _canvas.ToDataURLAsync());
            (bool success, string imageUrl) = AppState.CanvasHistory.TryUndo();
            if (success)
                _tempDataUrl = imageUrl;
            await InvokeAsync(StateHasChanged);
            await _context2D.ClearRectAsync(0, 0, _canvasSpecs.W, _canvasSpecs.H);
            await _context2D.DrawImageAsync("tempImage", 0, 0);
        }
        private async Task RedoAsync()
        {
            (bool success, string imageUrl) = AppState.CanvasHistory.TryRedo();
            if (success)
                _tempDataUrl = imageUrl;
            await InvokeAsync(StateHasChanged);
            await _context2D.ClearRectAsync(0, 0, _canvasSpecs.W, _canvasSpecs.H);
            await _context2D.DrawImageAsync("tempImage", 0, 0);
        }
        private async Task DrawCanvasAsync(double oldX, double oldY, double x, double y)
        {
            await using var ctx = await _context2D.CreateBatchAsync();
            await ctx.BeginPathAsync();
            await ctx.MoveToAsync(oldX, oldY);
            await ctx.LineToAsync(x, y);
            await ctx.StrokeAsync();
        }

        private void HandleNewSticky()
        {
            _currentNote = AppState.StickyNote;
            _selectedOption = "Sticky";
            StateHasChanged();
        }
        private void HandleNewImage()
        {
            _selectedOption = "Image";
            StateHasChanged();
        } 
        private async void UpdateState(object sender, PropertyChangedEventArgs e)
        {
            var prop = e.PropertyName;

            Task runTask = prop switch
            {
                nameof(AppState.Color) => HandleChangeColor(AppState.Color),
                nameof(AppState.IsEraseMode) => HandleToggleEraseMode(AppState.IsEraseMode),
                nameof(AppState.LineMode) => InvokeAsync(() => _isLineMode = AppState.LineMode),
                nameof(AppState.MarkerWidth) => HandleWidthChange(AppState.MarkerWidth),
                nameof(AppState.DblClkOption) => InvokeAsync(() => _selectedOption = AppState.DblClkOption),
                nameof(AppState.Text) => InvokeAsync(() => _text = AppState.Text),
                nameof(AppState.StickyNote) => InvokeAsync(HandleNewSticky),
                nameof(AppState.CurrentImage) => InvokeAsync(HandleNewImage),
                _ => InvokeAsync(StateHasChanged)
            };
            await runTask;
            //await InvokeAsync(StateHasChanged);
        }
        protected override bool ShouldRender()
        {
            if (!_shouldRender)
            {
                _shouldRender = true;
                return false;
            }
            return base.ShouldRender();
        }

        public void Dispose()
        {
            AppState.PropertyChanged -= UpdateState;
        }
    }
}
