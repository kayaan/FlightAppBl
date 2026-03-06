namespace FlightApp.Domain;

public class FixPoint
{
    public DateTime TimeUtc { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public int? AltitudeBaro { get; set; }
    public int? AltitudeGps { get; set; }
}