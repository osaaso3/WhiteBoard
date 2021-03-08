using Blazor.ModalDialog;
using Board.Client.Models;
using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board.Client.RazorComponents
{
    public partial class AddStickyNote
    {
        [Inject]
        private IModalDialogService ModalService { get; set; }
        private ElementReference _container;
        private Canvas _canvas;
        private Context2D _context2D;
        private Specs CanvasSpecs { get; set; } = new(400, 400);
        private StickyNote StickyNoteModel { get; set; } = new();
        private bool showImage;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _context2D = await _canvas.GetContext2DAsync();
                await _context2D.GlobalCompositeOperationAsync(CompositeOperation.Source_Over);
                await _context2D.StrokeStyleAsync("black");
                await _context2D.LineWidthAsync(2);
                await _context2D.LineJoinAsync(LineJoin.Round);
                await _context2D.LineCapAsync(LineCap.Square);
            }
            await base.OnAfterRenderAsync(firstRender);
        }
        private void SubmitRender()
        {
            var parameters = new ModalDialogParameters
            {
                {"StickyNoteModel",StickyNoteModel }
            };
            ModalService.Close(true, parameters);
        }
        private async Task RenderNote()
        {
            var headerFont = StickyNoteModel.FontSize * 1.5;
            var spec = StickyNoteModel.Size.AsSpecs();
            var colorImg = StickyNoteModel.BackgroundColor.ToImageName();
            await _context2D.DrawImageAsync(colorImg, 0, 0,128,128,0,0, spec.W, spec.H);
            var textLines = await RenderLines(StickyNoteModel.Text, spec.W/1.5);
            await using (Batch2D ctx = await _context2D.CreateBatchAsync())
            {
                //await ctx.DrawImageAsync(colorImg, 0, 0);
                await ctx.FontAsync($"bold {headerFont}px sarif");
                await ctx.FillTextAsync(StickyNoteModel.Header, 50, 35);
                //await ctx.BeginPathAsync();
                //await ctx.MoveToAsync(0, headerFont + 20);
                //await ctx.LineToAsync(spec.W, headerFont + 20);
                //await ctx.StrokeAsync();
                
                await ctx.FontAsync($"italic small-caps {StickyNoteModel.FontSize}px/{headerFont}px Georgia sarif");
                var start = headerFont + 50;
                foreach (var text in textLines)
                {
                    await ctx.StrokeTextAsync(text, 10, start);
                    start += headerFont;
                }
                
            }
            var imageData = await _canvas.ToDataURLAsync();
            StickyNoteModel.NoteImageData = Convert.FromBase64String(imageData.Split(',')[1]);
            await InvokeAsync(StateHasChanged);
        }
        private async Task<List<string>> RenderLines(string text, double maxwidth)
        {
            var words = text.Split(" ");
            var lines = new List<string>();
            var currentLine = words[0];
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var measure = await _context2D.MeasureTextAsync(currentLine + " " + word);
                var width = measure.Width;
                if (width < maxwidth)
                {
                    currentLine += $" {word}";
                }
                else
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
            }
            lines.Add(currentLine);
            return lines;
        }
    }
    public static class CanvasExtend
    {

    }
}
