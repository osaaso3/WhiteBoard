using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Board.Api
{
    public class ImageData
    {
        [JsonProperty("imageName")]
        public string ImageName { get; set; }
        [JsonProperty("imageBytes")]
        public byte[] ImageBytes { get; set; }
        [JsonProperty("imageText")]
        public string ImageText { get; set; }
        [JsonProperty("createdOnDate")]
        public DateTimeOffset? CreatedOnDate { get; set; }
    }
    public class ImageList
    {
        [JsonProperty("category")]
        public string Category { get; set; }
        [JsonProperty("images")]
        public List<ImageData> Images { get; set; }
    }
}
