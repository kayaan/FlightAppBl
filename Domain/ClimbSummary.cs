namespace FlightApp.Analysis;

/// <summary>
/// Summary statistics across all detected climb segments.
/// Each metric stores the value and the index of the climb it belongs to.
/// </summary>
public sealed class ClimbSummary
{
    public int Count { get; init; }

    public (double value, int index)? MaxDuration { get; set; }
    public (double value, int index)? MinDuration { get; set; }

    public (double value, int index)? MaxGain { get; set; }
    public (double value, int index)? MinGain { get; set; }

    public (double value, int index)? MaxAvgClimb { get; set; }
    public (double value, int index)? MinAvgClimb { get; set; }

    public (double value, int index)? MaxAvgSpeed { get; set; }
    public (double value, int index)? MinAvgSpeed { get; set; }

    public (double value, int index)? MaxSpeed { get; set; }

    public (double value, int index)? MaxClimb { get; set; }
    public (double value, int index)? MinClimb { get; set; }
}
