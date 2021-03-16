using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Board.Api
{
    public class ImageData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string UserName { get; set; }
        
        public string Category { get; set; } //ToDo change to Enum
        public ImageCategory ImageCategory { get; set; }
        [JsonProperty("imageName")]
        public string ImageName { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
       
        [JsonProperty("imageBytes")]
        public byte[] ImageBytes { get; set; }

        [JsonProperty("createdOnDate")]
        public DateTimeOffset? CreatedOnDate { get; set; }
    }
    public class ImageList
    {
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("category")]
        public string Category { get; set; } //ToDo change to Enum
        [JsonProperty("images")]
        public List<ImageData> Images { get; set; }
    }
    public enum ImageCategory
    {
        [Description("General image")]
        General,
        [Description("Whiteboard")]
        Whiteboard,
        [Description("Sticky Note")]
        StickyNote,

    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class DescriptionAttribute : Attribute
    {
        public string Text { get; }

        public DescriptionAttribute(string text)
        {
            Text = text;
        }
    }
}
