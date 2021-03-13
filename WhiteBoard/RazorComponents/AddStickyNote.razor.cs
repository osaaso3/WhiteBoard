using Blazor.ModalDialog;
using Board.Client.Models;
using Board.Client.Services.Interfaces;
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
        [Inject]
        private IStorageClient StorageClient { get; set; }
        private Canvas _canvas;
        private Context2D _context2D;
        private Specs CanvasSpecs { get; set; } = new(400, 400);
        private StickyNote StickyNoteModel { get; set; } = new() { FontSize = 12 };
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
        private async Task SaveToCloud(string username)
        {
            var image = new ImageData
            {
                ImageName = StickyNoteModel.Name,
                UserName = username,
                Category = "StickyNote",
                Description = StickyNoteModel.Header,
                ImageBytes = StickyNoteModel.NoteImageData
            };
            var response = await StorageClient.PostNewImage(username, image);
            Console.WriteLine($"Save to cloud response: {response}");
        }
        private void SubmitRender()
        {
            var parameters = new ModalDialogParameters
            {
                {"StickyNoteModel",StickyNoteModel }
            };
            ModalService.Close(true, parameters);
        }
        private async Task Render()
        {
            //ToDo Fix rendering so text is properly contained in stickynote image
            await RenderNote();
            await RenderNote();
        }
        private async Task RenderNote()
        {
            var headerFont = StickyNoteModel.FontSize * 1.5;
            var spec = StickyNoteModel.Size.AsSpecs();
            var colorImg = StickyNoteModel.BackgroundColor.ToImageName();
            await InvokeAsync(StateHasChanged);
            await _context2D.DrawImageAsync(colorImg, 0, 0, 128, 128, 0, 0, spec.W, spec.H);
            var textLines = await SplitRenderLines(StickyNoteModel.Text, spec.W * .8);

            await _context2D.FontAsync($"bold {headerFont}px sarif");
            await _context2D.FillTextAsync(StickyNoteModel.Header, 50, 25);

            await _context2D.FontAsync($"italic small-caps {StickyNoteModel.FontSize}px/{headerFont}px Georgia sarif");
            var start = headerFont + 50;
            foreach (var text in textLines)
            {
                await _context2D.StrokeTextAsync(text, 10, start);
                start += headerFont;
            }
            var imageData = await _canvas.ToDataURLAsync();
            StickyNoteModel.NoteImageData = Convert.FromBase64String(imageData.Split(',')[1]);
            await InvokeAsync(StateHasChanged);
        }
        private async Task<List<string>> SplitRenderLines(string text, double maxwidth)
        {
            var words = text.Split(" ");
            var lines = new List<string>();
            var currentLine = words[0].Replace("'", "`");

            for (int i = 1; i < words.Length; i++)
            {
                double width = 0;
                var word = words[i].Replace("'", "`");
                try
                {
                    var measure = await _context2D.MeasureTextAsync($"{currentLine} {word}");
                    width = measure.Width;
                    Console.WriteLine($"Success on word {i} ({word}): ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on word {i} ({word}): {ex.ToString().Substring(0, 10)}... ");
                }
                //var width = measure.Width;
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

}
