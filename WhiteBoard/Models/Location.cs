namespace Board.Client.Models
{
    public class Location
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
    public record CanvasSpecs(double H, double W);

}