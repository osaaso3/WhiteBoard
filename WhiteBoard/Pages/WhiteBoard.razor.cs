using System;
using System.Collections.Generic;
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
    public partial class WhiteBoard
    {
        [Inject]
        private IJSRuntime Js { get; set; }
        [Inject]
        private WhiteboardInterop Interop { get; set; }
        [Inject]
        private ISyncLocalStorageService LocalStorage { get; set; }
        private ElementReference _container;
        private Canvas _canvas;
        private Context2D _context2D;
        private Location _canvasLoc = new();
        private Location _oldMouseLocation = new();
        private Location _mouseLocation = new();
        private Location _touchLocation = new();
        private Location _oldTouchLocation = new();
        private double _canvasW = 1200;
        private double _canvasH = 600;
        private bool _isMouseDown;
        private string _color = "black";
        private bool _shouldRender = true;
        private double _lineWidth = 3;
        private bool _isEraseMode;
        private string _selectedOption;
        private string _text;
        private bool _isTouch;

        private string Pointer => _isEraseMode ? "erase-mode" : "marker-mode";
        [Parameter]
        public string Name { get; set; } = "new";
        [Parameter]
        public EventCallback<bool> IsStartNew { get; set; }
        [Parameter]
        public string DataUrl { get; set; }


        private async void HandleChangeColor(string color)
        {
            _color = string.IsNullOrWhiteSpace(color) ? "black" : color;
            await _context2D.StrokeStyleAsync(_color);
        }

        private async void HandleSave()
        {
            var imageUrl = await _canvas.ToDataURLAsync();
            var imageBytes = Convert.FromBase64String(imageUrl.Split(',')[1]);
            await Interop.DownloadProjectFile($"{Name}.png", imageBytes);
        }

        private async void HandleStartNew()
        {
            var imageUrl = await _canvas.ToDataURLAsync();
            var canvasData = new CanvasModel
            { Name = Name, ImageUrl = imageUrl, MarkerWidth = _lineWidth, Color = _color };
            LocalStorage.SetItem("LastCanvas", canvasData);
            await IsStartNew.InvokeAsync(true);

        }

        private async void HandleToggleEraseMode(bool isErase)
        {
            _isEraseMode = isErase;
            _color = isErase ? "white" : "black";
            _lineWidth = isErase ? 20 : 3;
            await _context2D.StrokeStyleAsync(_color);
            await _context2D.LineWidthAsync(_lineWidth);
        }

        private async void HandleWidthChange(double width)
        {
            _lineWidth = width;
            await _context2D.LineWidthAsync(_lineWidth);
        }
        private void HandleTextChange(string text)
        {
            _text = text;
        }
       
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _context2D = await _canvas.GetContext2DAsync();
                if (!string.IsNullOrWhiteSpace(DataUrl))
                {
                    await _context2D.DrawImageAsync("image", 0, 0);
                }
                // initialize settings
                await _context2D.GlobalCompositeOperationAsync(CompositeOperation.Source_Over);
                await _context2D.StrokeStyleAsync(_color);
                await _context2D.LineWidthAsync(_lineWidth);
                await _context2D.LineJoinAsync(LineJoin.Round);
                await _context2D.LineCapAsync(LineCap.Round);
                var containerLocation = await Interop.GetContainerLocation(_container.Id);
                (_canvasLoc.X, _canvasLoc.Y) = (containerLocation.X, containerLocation.Y);
                var width = await Interop.GetWindowSize();
                if (width <= 641)
                {
                    _canvasW = 600;
                    _canvasH = 300;
                }

            }
            Console.WriteLine($"color: {_color} line: {_lineWidth}");
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
        }
        private void MouseDownCanvas(MouseEventArgs e)
        {
            _shouldRender = false;
            _oldMouseLocation.X = _mouseLocation.X = e.ClientX - _canvasLoc.X;
            _oldMouseLocation.Y = _mouseLocation.Y = e.ClientY - _canvasLoc.Y;
            _isMouseDown = true;
        }

        private void MouseUpCanvas(MouseEventArgs e)
        {
            _shouldRender = false;
            _isMouseDown = false;
        }

        private async Task MouseMoveCanvasAsync(MouseEventArgs e)
        {
            _shouldRender = false;
            if (!_isMouseDown) return;
            _mouseLocation.X = e.ClientX - _canvasLoc.X;
            _mouseLocation.Y = e.ClientY - _canvasLoc.Y;
            await DrawCanvasAsync(_mouseLocation.X, _mouseLocation.Y, _oldMouseLocation.X, _oldMouseLocation.Y);
            _oldMouseLocation.X = _mouseLocation.X;
            _oldMouseLocation.Y = _mouseLocation.Y;
        }
        private void TouchStart(TouchEventArgs e)
        {
            _shouldRender = false;
            var touch = e.TargetTouches[0];
            _oldTouchLocation.X = _touchLocation.X = touch.ClientX - _canvasLoc.X;
            _oldTouchLocation.Y = _touchLocation.Y = touch.ClientY - _canvasLoc.Y;
            _isTouch = true;

        }
        private async Task TouchMoveAsync(TouchEventArgs e)
        {
            _shouldRender = false;
            if (!_isTouch) return;
            var touch = e.TargetTouches[0];
            _touchLocation.X = touch.ClientX - _canvasLoc.X;
            _touchLocation.Y = touch.ClientY - _canvasLoc.Y;
            await DrawCanvasAsync(_touchLocation.X, _touchLocation.Y, _oldTouchLocation.X, _oldTouchLocation.Y);
            _oldTouchLocation.X = _touchLocation.X;
            _oldTouchLocation.Y = _touchLocation.Y;
        }
        private void TouchEnd(TouchEventArgs e)
        {
            _shouldRender = false;
            _isTouch = false;
        }

        private async Task DrawCanvasAsync(double oldX, double oldY, double x, double y)
        {
            await using var ctx = await _context2D.CreateBatchAsync();
            await ctx.BeginPathAsync();
            await ctx.MoveToAsync(oldX, oldY);
            await ctx.LineToAsync(x, y);
            await ctx.StrokeAsync();
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
    }
}
