using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board.Client.Models;
using Microsoft.AspNetCore.Components;

namespace Board.Client.RazorComponents
{
    public partial class BoardMenu
    {
        private string ButtonLabel => IsEraseMode ? "Use Marker" : "Use Eraser";
        private string ButtonIcon => IsEraseMode ? "icons/eraser-32.png" : "icons/marker-32.png";
        private string LineButtonLabel => LineMode ? "Line Mode" : "Freestyle";
        private string LineButtonIcon => LineMode ? "icons/straight-line-32.png" : "icons/squiggly-line-32.png";
        private double selectedWidth = 3;
        private string _textInput;
        [Parameter]
        public string Color { get; set; }
        [Parameter]
        public EventCallback<string> ColorChanged { get; set; }
        [Parameter]
        public double MarkerWidth { get; set; }
        [Parameter]
        public EventCallback<double> MarkerWidthChanged { get; set; }
        private bool IsEraseMode { get; set; }
        [Parameter]
        public EventCallback<bool> IsEraseModeChanged { get; set; }
        [Parameter]
        public EventCallback SaveBoardAsImage { get; set; }
        [Parameter]
        public EventCallback StartNew { get; set; }
        [Parameter]
        public string Text { get; set; }
        [Parameter]
        public EventCallback<string> TextChanged { get; set; }
        [Parameter]
        public string DblClkOption { get; set; }
        [Parameter]
        public EventCallback<string> DblClkOptionChanged { get; set; }
        [Parameter]
        public bool LineMode { get; set; }
        [Parameter]
        public EventCallback<bool> LineModeChanged { get; set; }

        private void ChangeColor(ChangeEventArgs e)
        {
            Color = e.Value?.ToString() ?? "black";
            ColorChanged.InvokeAsync(Color);
        }
        private void ChangeText(ChangeEventArgs e)
        {
            Text = _textInput;
            TextChanged.InvokeAsync(Text);
        }
        private void ChangeSelectOption(ChangeEventArgs e)
        {
            DblClkOption = e.Value?.ToString() ?? "Text";
            DblClkOptionChanged.InvokeAsync(DblClkOption);
        }
        private void ToggleEraseMode()
        {
            IsEraseMode = !IsEraseMode;
            IsEraseModeChanged.InvokeAsync(IsEraseMode);
            if (LineMode) ToggleLineMode();            
        }
        private void ToggleLineMode()
        {
            LineMode = !LineMode;
            LineModeChanged.InvokeAsync(LineMode);            

        }
        private void ChangeWidth(ChangeEventArgs e)
        {
            //if (e.Value == null) return;
            MarkerWidth = selectedWidth;
            MarkerWidthChanged.InvokeAsync(MarkerWidth);
            Console.WriteLine($"Width changed to {e?.Value}");
        }
    }
}
