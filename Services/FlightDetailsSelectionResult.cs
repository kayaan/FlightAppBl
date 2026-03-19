using FlightApp.Domain;

namespace FlightApp.Services;

public sealed class FlightDetailsSelectionResult
{
    public int? SelectedClimbIndex { get; init; }
    public SelectionRange? CurrentSelection { get; init; }
    public FlightStats? DisplayedStats { get; init; }
    public bool ShowAllClimbs { get; init; }
}