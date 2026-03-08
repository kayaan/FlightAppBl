namespace FlightApp.Domain;

public class FlightImportResult
{
    public Flight Flight { get; set; } = new();
    public TrackArrays Track { get; set; } = new();
}