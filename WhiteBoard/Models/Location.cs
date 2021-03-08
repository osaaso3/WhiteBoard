namespace Board.Client.Models
{
    public class Location
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
    public record Specs(double H, double W);

}