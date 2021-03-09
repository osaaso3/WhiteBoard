using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Board.Client.Models
{
    public class ImageData
    {
        [JsonPropertyName("imageName")]
        public string ImageName { get; set; }
        [JsonPropertyName("imageBytes")]
        public byte[] ImageBytes { get; set; }
        [JsonPropertyName("imageText")]
        public string ImageText { get; set; }
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
    public static class ImageExtensions
    {
        public static string ToImageUrl(this byte[] buffer, string format = "image/png")
        {
            return $"data:{format};base64,{Convert.ToBase64String(buffer)}";
        }
    }
}
