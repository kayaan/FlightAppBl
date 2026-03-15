namespace FlightApp.Components;

public sealed class AllClimbsPayload
{
    public bool ShowAllClimbs { get; set; }

    public List<int>? Begin { get; set; }

    public List<int>? End { get; set; }
}