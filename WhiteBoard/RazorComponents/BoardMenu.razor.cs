using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.ModalDialog;
using Blazored.LocalStorage;
using Board.Client.Models;
using Board.Client.Services;
using Microsoft.AspNetCore.Components;

namespace Board.Client.RazorComponents
{
    public partial class BoardMenu
    {
        [Inject]
        private IModalDialogService ModalService { get; set; }
        [Inject]
        private AppState AppState { get; set; }
        [Inject]
        private ISyncLocalStorageService LocalStorage { get; set; }
        private string ButtonLabel => AppState.IsEraseMode ? "Use Marker" : "Use Eraser";
        private string ButtonIcon => AppState.IsEraseMode ? "icons/eraser-32.png" : "icons/marker-32.png";
        private string LineButtonLabel => AppState.LineMode ? "Line Mode" : "Freestyle";
        private string LineButtonIcon => AppState.LineMode ? "icons/straight-line-32.png" : "icons/squiggly-line-32.png";
        private double selectedWidth = 3;
        private string _textInput;
        
        private void ChangeColor(ChangeEventArgs e) => AppState.Color = e.Value?.ToString() ?? "black";
        private void ChangeText(ChangeEventArgs e) => AppState.Text = _textInput;
        private void ChangeSelectOption(ChangeEventArgs e) => AppState.DblClkOption = e.Value?.ToString() ?? "Text";
        private void ToggleEraseMode()
        {
            AppState.IsEraseMode = !AppState.IsEraseMode;
            if (AppState.LineMode) ToggleLineMode();
        }
        private void ToggleLineMode() => AppState.LineMode = !AppState.LineMode;
        private void SaveAsImage() => AppState.SaveBoardAsImage = !AppState.SaveBoardAsImage;
        private void StartNewInvoke() => AppState.StartNew = !AppState.StartNew;
        private void Clear() => AppState.ClearAndResize = !AppState.ClearAndResize;
        private void ChangeWidth(ChangeEventArgs e) => AppState.MarkerWidth = selectedWidth;
        private async Task ShowCreateStickyNote()
        {
            var options = new ModalDialogOptions
            {
                /*Style = "modal-base modal-large",*/ BackgroundClickToClose = true
            };
            ModalDialogResult modalResult = await ModalService.ShowDialogAsync<AddStickyNote>("Create a sticky note", options);
            
            var result = modalResult.ReturnParameters;
            var note = result.Get<StickyNote>("StickyNoteModel");
            var userNotes = LocalStorage.GetItem<UserStickyNotes>($"{AppState.UserName}-StickyNotes");
            AppState.UserStickyNotes ??= new UserStickyNotes { UserName = AppState.UserName };
            AppState.UserStickyNotes.StickyNotes ??= new List<StickyNote>();
            AppState.UserStickyNotes.StickyNotes.Add(note);
            LocalStorage.SetItem($"{AppState.UserName}-StickyNotes", AppState.UserStickyNotes);
            AppState.StickyNote = note;
        }

    }
}
