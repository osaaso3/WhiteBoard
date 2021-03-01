namespace Board.Client.Models
{
    public class CanvasModel
    {
        public string Name { get; init; }
        public string ImageUrl { get; set; }
        public string Color { get; set; }
        public double MarkerWidth { get; set; }
    }

    public enum MarkerWidth
    {
        ExtraThin = 1,
        Thin = 2,
        Standard = 3,
        Thick = 5,
        ExtraThick = 7
    }
}