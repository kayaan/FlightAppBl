using FlightApp.Analysis;
using FlightApp.Components;
using FlightApp.Domain;
using Microsoft.AspNetCore.Components;

namespace FlightApp.Services;

public sealed class FlightDetailsStateService
{
    public event Action? Changed;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    private Flight? _flight;
    public Flight? Flight
    {
        get => _flight;
        private set => SetField(ref _flight, value);
    }

    private TrackArrays? _trackArrays;
    public TrackArrays? TrackArrays
    {
        get => _trackArrays;
        private set => SetField(ref _trackArrays, value);
    }

    private FlightStats? _displayedStats;
    public FlightStats? DisplayedStats
    {
        get => _displayedStats;
        private set => SetField(ref _displayedStats, value);
    }

    private SelectionRange? _currentSelection;
    public SelectionRange? CurrentSelection
    {
        get => _currentSelection;
        private set => SetField(ref _currentSelection, value);
    }

    private List<ClimbSegment> _climbSegments = new();
    public List<ClimbSegment> ClimbSegments
    {
        get => _climbSegments;
        private set => SetField(ref _climbSegments, value);
    }

    private int? _selectedClimbIndex;
    public int? SelectedClimbIndex
    {
        get => _selectedClimbIndex;
        private set => SetField(ref _selectedClimbIndex, value);
    }

    private int? _hoveredClimbIndex;
    public int? HoveredClimbIndex
    {
        get => _hoveredClimbIndex;
        private set => SetField(ref _hoveredClimbIndex, value);
    }

    private bool _showAllClimbs;
    public bool ShowAllClimbs
    {
        get => _showAllClimbs;
        private set => SetField(ref _showAllClimbs, value);
    }

    private ClimbSummary? _climbSummary;
    public ClimbSummary? ClimbSummary
    {
        get => _climbSummary;
        private set => SetField(ref _climbSummary, value);
    }

    private List<ClimbSummaryKpi> _allClimbKpis = new();
    public List<ClimbSummaryKpi> AllClimbKpis
    {
        get => _allClimbKpis;
        private set => SetField(ref _allClimbKpis, value);
    }

    private MapStyle _mapStyle = MapStyle.Topo;
    public MapStyle MapStyle
    {
        get => _mapStyle;
        private set => SetField(ref _mapStyle, value);
    }

    private bool _isResizing;
    public bool IsResizing
    {
        get => _isResizing;
        private set => SetField(ref _isResizing, value);
    }

    private int _leftPanePercent = 50;
    public int LeftPanePercent
    {
        get => _leftPanePercent;
        private set => SetField(ref _leftPanePercent, value);
    }

    public ElementReference FlightLayoutRef { get; set; }

    // technische Werte -> kein Changed nötig
    public double LayoutLeft { get; set; }
    public double LayoutWidth { get; set; }

    public ClimbSegment? SelectedClimb
        => SelectedClimbIndex.HasValue &&
           SelectedClimbIndex.Value >= 0 &&
           SelectedClimbIndex.Value < ClimbSegments.Count
            ? ClimbSegments[SelectedClimbIndex.Value]
            : null;

    public ClimbSegment? HoveredClimb
        => HoveredClimbIndex.HasValue &&
           HoveredClimbIndex.Value >= 0 &&
           HoveredClimbIndex.Value < ClimbSegments.Count
            ? ClimbSegments[HoveredClimbIndex.Value]
            : null;

    public bool CanSelectPreviousClimb
        => SelectedClimbIndex.HasValue && SelectedClimbIndex.Value > 0;

    public bool CanSelectNextClimb
        => ClimbSegments.Count > 0 &&
           (!SelectedClimbIndex.HasValue || SelectedClimbIndex.Value < ClimbSegments.Count - 1);

    public bool CanClear
        => CurrentSelection is not null || SelectedClimbIndex is not null || ShowAllClimbs;

    public void ResetForLoad()
    {
        IsLoading = true;
        ErrorMessage = null;
        Flight = null;
        TrackArrays = null;
        DisplayedStats = null;
        CurrentSelection = null;
        ClimbSegments = new();
        SelectedClimbIndex = null;
        HoveredClimbIndex = null;
        ShowAllClimbs = false;
        AllClimbKpis = new();
        ClimbSummary = null;
    }

    public void SetLoadResult(
        Flight flight,
        TrackArrays trackArrays,
        FlightStats displayedStats,
        List<ClimbSegment> climbSegments,
        ClimbSummary? climbSummary,
        List<ClimbSummaryKpi> allClimbKpis)
    {
        Flight = flight;
        TrackArrays = trackArrays;
        DisplayedStats = displayedStats;
        ClimbSegments = climbSegments;
        ClimbSummary = climbSummary;
        AllClimbKpis = allClimbKpis;
    }

    public void SetLoadingFinished()
    {
        IsLoading = false;
    }

    public void SetError(string message)
    {
        ErrorMessage = message;
    }

    public void SetHoveredClimb(int index)
    {
        if (SelectedClimbIndex == index)
        {
            HoveredClimbIndex = null;
            return;
        }

        HoveredClimbIndex = index;
    }

    public void ClearHoveredClimb()
    {
        HoveredClimbIndex = null;
    }


    public void SetShowAllClimbs(bool value)
    {
        if (_showAllClimbs == value)
            return;

        _showAllClimbs = value;

        if (value)
        {
            _selectedClimbIndex = null;
            _hoveredClimbIndex = null;
        }

        NotifyChanged();
    }

    public void ApplySelectionResult(FlightDetailsSelectionResult result)
    {
        var changed = false;

        if (result.DisplayedStats is not null &&
            !EqualityComparer<FlightStats?>.Default.Equals(_displayedStats, result.DisplayedStats))
        {
            _displayedStats = result.DisplayedStats;
            changed = true;
        }

        if (_selectedClimbIndex != result.SelectedClimbIndex)
        {
            _selectedClimbIndex = result.SelectedClimbIndex;
            changed = true;
        }

        if (!Equals(_currentSelection, result.CurrentSelection))
        {
            _currentSelection = result.CurrentSelection;
            changed = true;
        }

        if (_showAllClimbs != result.ShowAllClimbs)
        {
            _showAllClimbs = result.ShowAllClimbs;
            changed = true;
        }

        if (changed)
            NotifyChanged();
    }

    public void ClearHoverOnSelection()
    {
        if (HoveredClimbIndex is null)
            return;

        HoveredClimbIndex = null;
    }

    public void ToggleShowAllClimbs()
    {
        var newValue = !ShowAllClimbs;
        var changed = false;

        if (newValue)
        {
            if (SelectedClimbIndex is not null)
            {
                _selectedClimbIndex = null;
                changed = true;
            }

            if (HoveredClimbIndex is not null)
            {
                _hoveredClimbIndex = null;
                changed = true;
            }
        }

        if (_showAllClimbs != newValue)
        {
            _showAllClimbs = newValue;
            changed = true;
        }

        if (changed)
            NotifyChanged();
    }

    public void ClearSelectedClimb()
    {
        if (SelectedClimbIndex is null)
            return;

        SelectedClimbIndex = null;
    }

    public void SetMapStyle(MapStyle style)
    {
        MapStyle = style;
    }

    public void SetIsResizing(bool isResizing)
    {
        IsResizing = isResizing;
    }

    public bool SetLeftPanePercent(int percent)
    {
        if (LeftPanePercent == percent)
            return false;

        LeftPanePercent = percent;
        return true;
    }

    private bool SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        NotifyChanged();
        return true;
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }

    public int? FindClimbIndexAtTrackIndex(int trackIndex)
    {
        for (int i = 0; i < ClimbSegments.Count; i++)
        {
            var climb = ClimbSegments[i];

            if (trackIndex >= climb.BeginIndex && trackIndex <= climb.EndIndex)
                return i;
        }

        return null;
    }

    public void SetHoveredClimbFromTrackIndex(int trackIndex)
    {
        if (!ShowAllClimbs)   // 🔥 FIX
        {
            if (_hoveredClimbIndex != null)
            {
                _hoveredClimbIndex = null;
                NotifyChanged();
            }
            return;
        }

        var climbIndex = FindClimbIndexAtTrackIndex(trackIndex);

        if (!climbIndex.HasValue || SelectedClimbIndex == climbIndex.Value)
        {
            if (_hoveredClimbIndex != null)
            {
                _hoveredClimbIndex = null;
                NotifyChanged();
            }
            return;
        }

        if (_hoveredClimbIndex != climbIndex.Value)
        {
            _hoveredClimbIndex = climbIndex.Value;
            NotifyChanged();
        }
    }
}