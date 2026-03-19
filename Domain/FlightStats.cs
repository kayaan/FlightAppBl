namespace FlightApp.Domain;


public class FlightStats
{ 
    public bool IsSegmentSelection { get; set; }

    public int? SelectionStartIndex { get; set; }
    public int? SelectionEndIndex { get; set; }

    public int FixCount { get; set; }

    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }

    public TimeSpan Duration { get; set; }

    public double TotalDistanceMeters { get; set; }

    public int? AltGpsStart { get; set; }
    public int? AltGpsEnd { get; set; }
    public int? AltGpsMin { get; set; }
    public int? AltGpsMax { get; set; }

    public int? AltBaroStart { get; set; }
    public int? AltBaroEnd { get; set; }
    public int? AltBaroMin { get; set; }
    public int? AltBaroMax { get; set; }

    public double? AvgGroundSpeedKmh { get; set; }
    public double? MaxGroundSpeedKmh { get; set; }

    public double? AvgVarioMs { get; set; }
    public double? MaxVarioMs { get; set; }
    public double? MinVarioMs { get; set; }

    public double TotalClimbMeters { get; set; }
    public double TotalSinkMeters { get; set; }


    public int? AltGpsGainMeters { get; set; }
    public int? AltGpsLossMeters { get; set; }

    public int? AltBaroGainMeters { get; set; }
    public int? AltBaroLossMeters { get; set; }

    public int? MaxHeightAboveLaunchGps { get; set; }
    public int? MaxHeightAboveLaunchBaro { get; set; }

    public int? MaxHeightAboveLandingGps { get; set; }
    public int? MaxHeightAboveLandingBaro { get; set; }
}