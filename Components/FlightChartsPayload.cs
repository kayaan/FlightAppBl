namespace FlightApp.Components;

public sealed class FlightChartsPayload
{
    public double[] TimeSec { get; set; } = Array.Empty<double>();

    public string AltitudeTitle { get; set; } = "";
    public string AltitudeUnit { get; set; } = "";
    public double[]? AltitudeValues { get; set; }

    public string VarioTitle { get; set; } = "";
    public string VarioUnit { get; set; } = "";
    public double[]? VarioValues { get; set; }

    public string SpeedTitle { get; set; } = "";
    public string SpeedUnit { get; set; } = "";
    public double[]? SpeedValues { get; set; }

    public int? SelectedClimbBeginIndex { get; set; }
    public int? SelectedClimbEndIndex { get; set; }
}