using FlightApp.Analysis;
using FlightApp.Domain;
using Microsoft.AspNetCore.Components;

namespace FlightApp.Components;

public sealed class FlightDetailsState
{
    public SelectionRange? CurrentSelection { get; set; }
    public FlightStats? DisplayedStats { get; set; }

    public Flight? Flight { get; set; }
    public TrackArrays? TrackArrays { get; set; }
    public List<ClimbSegment> ClimbSegments { get; set; } = new();
    public int? SelectedClimbIndex { get; set; }

    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }

    public int LeftPanePercent { get; set; } = 50;
    public bool IsResizing { get; set; }

    public ElementReference FlightLayoutRef { get; set; }
    public double LayoutLeft { get; set; }
    public double LayoutWidth { get; set; }

    public ClimbSummary? ClimbSummary { get; set; }

    public List<ClimbSummaryKpi> AllClimbKpis { get; set; } = new();

    public bool ShowAllClimbs { get; set; }
}