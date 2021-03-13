using Board.Client.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Board.Client.Services
{
    public class AppState : INotifyPropertyChanged
    {
        private string color;
        private double markerWidth;
        private bool isEraseMode;
        private string text;
        private string dblClkOption;
        private bool lineMode;
        private StickyNote stickyNote;
        private ImageData currentImage;

        public event PropertyChangedEventHandler PropertyChanged;
        public string UserName { get; set; }
        public bool IsAuth { get; set; }
        public UserStickyNotes UserStickyNotes { get; set; }
        public CanvasHistory<string> CanvasHistory { get; set; } = new(10);
        public string Color
        {
            get => color;
            set { color = value; OnPropertyChanged(); }
        }
        public double MarkerWidth
        {
            get => markerWidth;
            set { markerWidth = value; OnPropertyChanged(); }
        }
        public bool IsEraseMode
        {
            get => isEraseMode;
            set { isEraseMode = value; OnPropertyChanged(); }
        }

        public string Text
        {
            get => text;
            set { text = value; OnPropertyChanged(); }
        }
        public string DblClkOption
        {
            get => dblClkOption;
            set { dblClkOption = value; OnPropertyChanged(); }
        }
        public bool LineMode
        {
            get => lineMode;
            set { lineMode = value; OnPropertyChanged(); }
        }
        public StickyNote StickyNote
        {
            get => stickyNote;
            set { stickyNote = value; OnPropertyChanged(); }
        }
        public ImageData CurrentImage
        {
            get => currentImage;
            set { currentImage = value; OnPropertyChanged(); }
        }
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
