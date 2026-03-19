namespace FlightApp.Components;

public sealed class HoveredClimbPayload
{
    public int? HoveredClimbBeginSec { get; set; }
    
    public int? HoveredClimbEndSec { get; set; }

    public int? HoveredClimbIndex { get; set; }
}