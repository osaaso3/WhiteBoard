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
        [Parameter]
        public EventCallback Undo { get; set; }
        [Parameter]
        public EventCallback Redo { get; set; }
        [Parameter]
        public EventCallback SaveAsImage { get; set; }
        [Parameter]
        public EventCallback Clear { get; set; }
        [Parameter]
        public EventCallback StartNew { get; set; }

        private string ButtonLabel => AppState.IsEraseMode ? "Use Marker" : "Use Eraser";
        private string ButtonIcon => AppState.IsEraseMode ? "icons/eraser-32.png" : "icons/marker-32.png";
        private string LineButtonLabel => AppState.LineMode ? "Line Mode" : "Freestyle";
        private string LineButtonIcon => AppState.LineMode ? "icons/straight-line-32.png" : "icons/squiggly-line-32.png";
        private double selectedWidth = 3;
        private string _textInput;
        private string _selectOption;
        private void ChangeColor(ChangeEventArgs e) => AppState.Color = e.Value?.ToString() ?? "black";
        private void ChangeText(ChangeEventArgs e) => AppState.Text = _textInput;
        private void ChangeSelectOption(ChangeEventArgs e) => AppState.DblClkOption = e.Value?.ToString() ?? "Text";
        private void ToggleEraseMode()
        {
            AppState.IsEraseMode = !AppState.IsEraseMode;
            if (AppState.LineMode) ToggleLineMode();
        }
        private void ToggleLineMode() => AppState.LineMode = !AppState.LineMode;
        private void SaveAsImageInvoke() => SaveAsImage.InvokeAsync();
        private void StartNewInvoke() => StartNew.InvokeAsync();
        private void ClearInvoke() => Clear.InvokeAsync();
        private void ChangeWidth(ChangeEventArgs e) => AppState.MarkerWidth = selectedWidth;
        private void UndoInvoke() => Undo.InvokeAsync();
        private void RedoInvoke() => Redo.InvokeAsync();
        private async Task ShowCreateStickyNote()
        {
            var options = new ModalDialogOptions
            {
                Style = "modal-base",
                BackgroundClickToClose = false
            };
            ModalDialogResult modalResult = await ModalService.ShowDialogAsync<AddStickyNote>("Create a sticky note", options);

            var result = modalResult.ReturnParameters;
            if (!modalResult.Success) return;
            var note = result.Get<StickyNote>("StickyNoteModel");
            var userNotes = LocalStorage.GetItem<UserStickyNotes>($"{AppState.UserName}-StickyNotes");
            if (userNotes == null || userNotes?.StickyNotes?.Count == 0)
            {
                AppState.UserStickyNotes ??= new UserStickyNotes { UserName = AppState.UserName };
                AppState.UserStickyNotes.StickyNotes ??= new List<StickyNote>();
            }
            else
            {
                AppState.UserStickyNotes = userNotes;
            }            
            AppState.UserStickyNotes.StickyNotes.Add(note);
            _selectOption = "Sticky";
            LocalStorage.SetItem($"{AppState.UserName}-StickyNotes", AppState.UserStickyNotes);
            AppState.StickyNote = note;
        }

    }
}
