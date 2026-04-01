namespace FlightApp.Components;

public sealed class HoveredClimbMapPayload
{
    public int? BeginIndex { get; set; }

    public int? EndIndex { get; set; }

    public int? ClimbIndex { get; set; }

    public bool IsSameAsSelected { get; set; }
}