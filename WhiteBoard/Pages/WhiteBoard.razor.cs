using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Board.Client.Models;
using Board.Client.Services;
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
            var imageUrl = await _canvas.ToDataURLAsync();
            var imageBytes = Convert.FromBase64String(imageUrl.Split(',')[1]);
            await Interop.DownloadProjectFile($"{Name}.png", imageBytes);
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
            await SaveCanvasStateToLocalStorage();
            var confirm = await Js.InvokeAsync<bool>("confirm", "Do you want to clear the whiteboard?");
            if (confirm)
            {
                await _context2D.ClearRectAsync(0, 0, _canvasSpecs.W, _canvasSpecs.H);
                var specs = await Interop.GetCanvasSize(_container.Id);
                _canvasSpecs = new Specs(specs.H, specs.W);
                AppState.CanvasHistory.Clear();
                await InvokeAsync(StateHasChanged);
            }
        }
        

        private async Task SaveCanvasStateToLocalStorage()
        {
            var imageUrl = await _canvas.ToDataURLAsync();
            var canvasData = new CanvasModel
            { Name = Name, ImageUrl = imageUrl, MarkerWidth = _lineWidth, Color = _color };
            //AppState.CanvasHistory.Push(imageUrl);
           
            LocalStorage.SetItem("LastCanvas", canvasData);
            LocalStorage.SetItem("CurrentHistory", AppState.CanvasHistory);
        }
        private void SaveToHistory(string imageUrl)
        {
            //try
            //{
            //    if (AppState.CanvasHistory.Peek() == imageUrl) return;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
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
            _tempDataUrl = await _canvas.ToDataURLAsync();
            SaveToHistory(_tempDataUrl);
        }
        private async Task MouseDownCanvas(MouseEventArgs e)
        {
            _shouldRender = false;
            _oldMouseLocation.X = _mouseLocation.X = e.ClientX - _canvasLoc.X;
            _oldMouseLocation.Y = _mouseLocation.Y = e.ClientY - _canvasLoc.Y;
            _isMouseDown = true;
            //if (!_isLineMode) return;
            _tempDataUrl = await _canvas.ToDataURLAsync();
            _isRecord = true;
            //SaveToHistory(_tempDataUrl);
        }

        private async Task MouseUpCanvas(MouseEventArgs e)
        {
            _shouldRender = false;
            if (_isMouseDown)
            {
                _tempDataUrl = await _canvas.ToDataURLAsync();
                SaveToHistory(_tempDataUrl);
            }
            _isMouseDown = false;
            
        }

        private async Task MouseMoveCanvasAsync(MouseEventArgs e)
        {
            _shouldRender = false;
            if (!_isMouseDown) return;
            //if (_isRecord)
            //{
            //    _isRecord = false;
            //    SaveToHistory(_tempDataUrl);
            //}

            _mouseLocation.X = e.ClientX - _canvasLoc.X;
            _mouseLocation.Y = e.ClientY - _canvasLoc.Y;
            if (_isLineMode)
            {
                await _context2D.ClearRectAsync(0, 0, _canvasSpecs.W, _canvasSpecs.H);
                await _context2D.DrawImageAsync("tempImage", 0, 0);
            }
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
            //if (!_isLineMode) return;
            _tempDataUrl = await _canvas.ToDataURLAsync();
            _isRecord = true;
            //SaveToHistory(_tempDataUrl);
        }
        private async Task TouchMoveAsync(TouchEventArgs e)
        {
            _shouldRender = false;
            if (!_isTouch) return;
            if (_isRecord)
            {
                _isRecord = false;
               
                SaveToHistory(_tempDataUrl);
            }
            var touch = e.TargetTouches[0];
            _touchLocation.X = touch.ClientX - _canvasLoc.X;
            _touchLocation.Y = touch.ClientY - _canvasLoc.Y;
            if (_isLineMode)
            {
                await _context2D.ClearRectAsync(0, 0, _canvasSpecs.W, _canvasSpecs.H);
                await _context2D.DrawImageAsync("tempImage", 0, 0);
            }
            await DrawCanvasAsync(_touchLocation.X, _touchLocation.Y, _oldTouchLocation.X, _oldTouchLocation.Y);
            if (_isLineMode) return;
            _oldTouchLocation.X = _touchLocation.X;
            _oldTouchLocation.Y = _touchLocation.Y;
        }
        private void TouchEnd(TouchEventArgs e)
        {            
            _shouldRender = false;
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
                nameof(AppState.ClearAndResize) => HandleClearAndResize(),
                nameof(AppState.SaveBoardAsImage) => HandleSave(),
                nameof(AppState.Text) => InvokeAsync(() => _text = AppState.Text),
                nameof(AppState.StartNew) => HandleStartNew(),
                nameof(AppState.StickyNote) => InvokeAsync(HandleNewSticky),
                nameof(AppState.Undo) => UndoAsync(),
                nameof(AppState.Redo) => RedoAsync(),
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
