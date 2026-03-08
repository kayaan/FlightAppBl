namespace FlightApp.Domain;


public class TrackArrays
{
    public int[] TDeltaMs { get; set; } = Array.Empty<int>();

    public int[] LatE7 { get; set; } = Array.Empty<int>();
    public int[] LonE7 { get; set; } = Array.Empty<int>();

    public int[] AltGpsCm { get; set; } = Array.Empty<int>();
    public int[] AltBaroCm { get; set; } = Array.Empty<int>();

    public int[] SpeedCms { get; set; } = Array.Empty<int>();
    public int[] VarioCms { get; set; } = Array.Empty<int>();
}