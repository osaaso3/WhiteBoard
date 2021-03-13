using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Board.Client.Models
{
    public class StickyNote
    {
        public string Name { get; set; }
        public string Text { get; set; }
        [Required]
        public NoteColor BackgroundColor { get; set; }
        [Required]
        public NoteSize Size { get; set; }
        public string FontStyle { get; set; }
        [Range(6, 48, ErrorMessage = "Font size must be a value between 6 and 48")]
        public int FontSize { get; set; }
        public string Header { get; set; }
        public byte[] NoteImageData { get; set; }
        public string ImageUrl => NoteImageData?.ToImageUrl();
    }
    public class UserStickyNotes
    {
        public string UserName { get; set; }
        public List<StickyNote> StickyNotes { get; set; }
    }
    public enum NoteSize
    {
        Medium, ExtraSmall, Small, Large, ExtraLarge
    }
    public enum NoteColor
    {
        Blue, Green, LightBlue, LightGreen, Orange, Pink, Purple, Teal, Yellow
    }
    public static class NoteExtensions
    {
        private static readonly Dictionary<NoteSize, Specs> noteSpecs = new()
        {
            { NoteSize.ExtraSmall, new Specs(96, 96) },
            { NoteSize.Small, new Specs(160, 160) },
            { NoteSize.Medium, new Specs(224, 224) },
            { NoteSize.Large, new Specs(288, 288) },
            { NoteSize.ExtraLarge, new Specs(352, 352) }
        };
        public static Specs AsSpecs(this NoteSize noteSize)
        {
            return noteSpecs.TryGetValue(noteSize, out var val) ? val : new Specs(0, 0);
        }
        public static string ToImageName(this NoteColor noteColor)
        {
            return Enum.GetName(noteColor).ToLower();
        }
    }
}
