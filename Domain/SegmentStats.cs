namespace FlightApp.Domain;

public sealed class SegmentStats
{
    public TimeSpan Duration { get; init; }
    public double DistanceKm { get; init; }

    public double AltitudeMinM { get; init; }
    public double AltitudeMaxM { get; init; }

    public double VarioMinMs { get; init; }
    public double VarioMaxMs { get; init; }

    public double SpeedAvgKmh { get; init; }
    public double SpeedMaxKmh { get; init; }
}