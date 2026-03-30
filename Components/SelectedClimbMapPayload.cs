namespace FlightApp.Components;

public sealed class SelectedClimbMapPayload 
{
    public int? BeginIndex { get; set; }
    public int? EndIndex { get; set; }

    public int? CursorIndex { get; set; }
}