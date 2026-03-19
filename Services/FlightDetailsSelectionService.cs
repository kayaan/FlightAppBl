using FlightApp.Analysis;
using FlightApp.Domain;

namespace FlightApp.Services;

public sealed class FlightDetailsSelectionService
{
    private readonly TrackSegmentStatsCalculator segmentStatsCalculator = new();

    // =========================
    // APPLY SELECTION
    // =========================
    public FlightDetailsSelectionResult ApplySelection(
        TrackArrays trackArrays,
        int startIndex,
        int endIndex)
    {
        var selection = new SelectionRange(startIndex, endIndex);

        if (!selection.IsValid)
            return new FlightDetailsSelectionResult();

        var stats = segmentStatsCalculator.Calculate(trackArrays, selection);

        return new FlightDetailsSelectionResult
        {
            SelectedClimbIndex = null,
            CurrentSelection = selection,
            DisplayedStats = stats,
            ShowAllClimbs = false
        };
    }

    // =========================
    // SELECT CLIMB
    // =========================
    public FlightDetailsSelectionResult SelectClimb(
        TrackArrays trackArrays,
        List<ClimbSegment> climbs,
        int index)
    {
        if (index < 0 || index >= climbs.Count)
            return new FlightDetailsSelectionResult();

        var climb = climbs[index];

        var selection = new SelectionRange(climb.BeginIndex, climb.EndIndex);
        var stats = segmentStatsCalculator.Calculate(trackArrays, selection);

        return new FlightDetailsSelectionResult
        {
            SelectedClimbIndex = index,
            CurrentSelection = selection,
            DisplayedStats = stats,
            ShowAllClimbs = false
        };
    }

    // =========================
    // NEXT
    // =========================
    public FlightDetailsSelectionResult SelectNextClimb(
        TrackArrays trackArrays,
        List<ClimbSegment> climbs,
        int? currentIndex)
    {
        if (climbs.Count == 0)
            return new FlightDetailsSelectionResult();

        if (!currentIndex.HasValue)
            return SelectClimb(trackArrays, climbs, 0);

        if (currentIndex.Value >= climbs.Count - 1)
            return new FlightDetailsSelectionResult();

        return SelectClimb(trackArrays, climbs, currentIndex.Value + 1);
    }

    // =========================
    // PREVIOUS
    // =========================
    public FlightDetailsSelectionResult SelectPreviousClimb(
        TrackArrays trackArrays,
        List<ClimbSegment> climbs,
        int? currentIndex)
    {
        if (!currentIndex.HasValue || currentIndex.Value <= 0)
            return new FlightDetailsSelectionResult();

        return SelectClimb(trackArrays, climbs, currentIndex.Value - 1);
    }

    // =========================
    // CLEAR
    // =========================
    public FlightDetailsSelectionResult ClearSelection(Flight? flight)
    {
        return new FlightDetailsSelectionResult
        {
            SelectedClimbIndex = null,
            CurrentSelection = null,
            DisplayedStats = flight?.Stats,
            ShowAllClimbs = false
        };
    }
}