using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Board.Client.Models
{
    public class ImageData
    {
        private ImageCategory imageCategory;

        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
        [JsonPropertyName("category")]
        public string Category { get; set; } //ToDo change to Enum
        [JsonPropertyName("imageCategory")]
        public ImageCategory ImageCategory
        {
            get
            {
                if (imageCategory != ImageCategory.None) return imageCategory;
                return Category switch
                {
                    "General" => ImageCategory.General,
                    "StickyNote" => ImageCategory.StickyNote,
                    "Whiteboard" => ImageCategory.Whiteboard,
                    _ => ImageCategory.None
                };
            }
            set { imageCategory = value; }
        }
        [JsonPropertyName("imageName")]
        public string ImageName { get; set; }
        [JsonPropertyName("imageBytes")]
        public byte[] ImageBytes { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("createdOnDate")]
        public DateTimeOffset? CreatedOnDate { get; set; }
    }
    public class ImageList
    {
        [JsonPropertyName("category")]
        public string Category { get; set; }
        [JsonPropertyName("images")]
        public List<ImageData> Images { get; set; }
    }
    public enum ImageCategory
    {
        None,
        [Description("General")]
        General,
        [Description("Whiteboard")]
        Whiteboard,
        [Description("StickyNote")]
        StickyNote,

    }
    
}
