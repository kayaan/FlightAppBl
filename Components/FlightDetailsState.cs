using FlightApp.Analysis;
using FlightApp.Domain;
using Microsoft.AspNetCore.Components;

namespace FlightApp.Components;

public sealed class FlightDetailsState
{
    // =========================================================
    // DATA STATE (Loaded domain data)
    // =========================================================

    public Flight? Flight { get; set; }

    public TrackArrays? TrackArrays { get; set; }

    public FlightStats? DisplayedStats { get; set; }


    // =========================================================
    // SELECTION STATE (User selection / chart interaction)
    // =========================================================

    public SelectionRange? CurrentSelection { get; set; }


    // =========================================================
    // CLIMB STATE (Detected climbs + navigation)
    // =========================================================

    public List<ClimbSegment> ClimbSegments { get; set; } = new();

    public int? SelectedClimbIndex { get; set; }

    public ClimbSummary? ClimbSummary { get; set; }

    public List<ClimbSummaryKpi> AllClimbKpis { get; set; } = new();

    public bool ShowAllClimbs { get; set; }


    // =========================================================
    // MAP STATE (Basemap / map UI options)
    // =========================================================

    public MapStyle MapStyle { get; set; } = MapStyle.Topo;


    // =========================================================
    // UI STATE (Loading, errors)
    // =========================================================

    public bool IsLoading { get; set; }

    public string? ErrorMessage { get; set; }


    // =========================================================
    // LAYOUT STATE (Splitter / resizing)
    // =========================================================

    public int LeftPanePercent { get; set; } = 50;

    public bool IsResizing { get; set; }

    public ElementReference FlightLayoutRef { get; set; }

    public double LayoutLeft { get; set; }

    public double LayoutWidth { get; set; }
}