using FlightApp.Analysis;
using FlightApp.Domain;

namespace FlightApp.Services;

public sealed class FlightDetailsLoadResult
{
    public Flight? Flight { get; init; }
    public TrackArrays? TrackArrays { get; init; }
    public FlightStats? DisplayedStats { get; init; }

    public List<ClimbSegment> ClimbSegments { get; init; } = new();
    public ClimbSummary? ClimbSummary { get; init; }
    public List<ClimbSummaryKpi> AllClimbKpis { get; init; } = new();
}