using FlightApp.Analysis;
using FlightApp.Components;
using FlightApp.Domain;

namespace FlightApp.Services;

public sealed class FlightDetailsLoadService
{
    private readonly FlightService flightService;
    private readonly TrackSegmentStatsCalculator segmentStatsCalculator = new();

    public FlightDetailsLoadService(FlightService flightService)
    {
        this.flightService = flightService;
    }

    public async Task<FlightDetailsLoadResult> LoadAsync(string flightId)
    {
        var flight = await flightService.GetFlightByIdAsync(flightId);
        var trackArrays = await flightService.GetTrackAsync(flightId);

        var displayedStats = flight?.Stats;
        var climbSegments = new List<ClimbSegment>();
        ClimbSummary? climbSummary = null;
        var allClimbKpis = new List<ClimbSummaryKpi>();

        if (trackArrays is not null)
        {
            climbSegments = ClimbDetector.DetectClimbs(trackArrays);
            climbSummary = ComputeClimbSummary(trackArrays, climbSegments);
            allClimbKpis = BuildAllClimbKpis(climbSummary);
        }

        return new FlightDetailsLoadResult
        {
            Flight = flight,
            TrackArrays = trackArrays,
            DisplayedStats = displayedStats,
            ClimbSegments = climbSegments,
            ClimbSummary = climbSummary,
            AllClimbKpis = allClimbKpis
        };
    }

    private ClimbSummary ComputeClimbSummary(TrackArrays trackArrays, List<ClimbSegment> climbSegments)
    {
        var summary = new ClimbSummary
        {
            Count = climbSegments.Count
        };

        for (int i = 0; i < climbSegments.Count; i++)
        {
            var climb = climbSegments[i];

            var duration = climb.DurationSec;
            var gain = climb.GainM;
            var avgClimb = climb.AvgClimbRateMs;
            var avgSpeed = climb.AvgSpeedKmh;
            var maxSpeed = climb.MaxSpeedKmh;

            if (summary.MaxDuration is null || duration > summary.MaxDuration.Value.value)
                summary.MaxDuration = (duration, i);

            if (summary.MinDuration is null || duration < summary.MinDuration.Value.value)
                summary.MinDuration = (duration, i);

            if (summary.MaxGain is null || gain > summary.MaxGain.Value.value)
                summary.MaxGain = (gain, i);

            if (summary.MinGain is null || gain < summary.MinGain.Value.value)
                summary.MinGain = (gain, i);

            if (summary.MaxAvgClimb is null || avgClimb > summary.MaxAvgClimb.Value.value)
                summary.MaxAvgClimb = (avgClimb, i);

            if (summary.MinAvgClimb is null || avgClimb < summary.MinAvgClimb.Value.value)
                summary.MinAvgClimb = (avgClimb, i);

            if (summary.MaxAvgSpeed is null || avgSpeed > summary.MaxAvgSpeed.Value.value)
                summary.MaxAvgSpeed = (avgSpeed, i);

            if (summary.MinAvgSpeed is null || avgSpeed < summary.MinAvgSpeed.Value.value)
                summary.MinAvgSpeed = (avgSpeed, i);

            if (summary.MaxSpeed is null || maxSpeed > summary.MaxSpeed.Value.value)
                summary.MaxSpeed = (maxSpeed, i);

            var selection = new SelectionRange(climb.BeginIndex, climb.EndIndex);
            var stats = segmentStatsCalculator.Calculate(trackArrays, selection);

            var maxClimb = stats?.MaxVarioMs;
            var minClimb = stats?.MinVarioMs;

            if (maxClimb.HasValue)
            {
                if (summary.MaxClimb is null || maxClimb.Value > summary.MaxClimb.Value.value)
                    summary.MaxClimb = (maxClimb.Value, i);
            }

            if (minClimb.HasValue)
            {
                if (summary.MinClimb is null || minClimb.Value < summary.MinClimb.Value.value)
                    summary.MinClimb = (minClimb.Value, i);
            }
        }

        return summary;
    }

    private static List<ClimbSummaryKpi> BuildAllClimbKpis(ClimbSummary? climbSummary)
    {
        var items = new List<ClimbSummaryKpi>();

        if (climbSummary is null || climbSummary.Count == 0)
            return items;

        if (climbSummary.MaxDuration is { } maxDuration &&
            climbSummary.MinDuration is { } minDuration)
        {
            items.Add(new ClimbSummaryKpi
            {
                Label = "Duration",
                MaxText = FlightDetailsFormatters.FormatDurationSeconds(maxDuration.value),
                MaxClimbIndex = maxDuration.index,
                MinText = FlightDetailsFormatters.FormatDurationSeconds(minDuration.value),
                MinClimbIndex = minDuration.index,
                SubText = "Max / Min"
            });
        }

        if (climbSummary.MaxGain is { } maxGain &&
            climbSummary.MinGain is { } minGain)
        {
            items.Add(new ClimbSummaryKpi
            {
                Label = "Gain",
                MaxText = FlightDetailsFormatters.FormatMeters(maxGain.value),
                MaxClimbIndex = maxGain.index,
                MinText = FlightDetailsFormatters.FormatMeters(minGain.value),
                MinClimbIndex = minGain.index,
                SubText = "Max / Min"
            });
        }

        if (climbSummary.MaxAvgClimb is { } maxAvgClimb &&
            climbSummary.MinAvgClimb is { } minAvgClimb)
        {
            items.Add(new ClimbSummaryKpi
            {
                Label = "Avg Climb",
                MaxText = FlightDetailsFormatters.FormatMs(maxAvgClimb.value),
                MaxClimbIndex = maxAvgClimb.index,
                MinText = FlightDetailsFormatters.FormatMs(minAvgClimb.value),
                MinClimbIndex = minAvgClimb.index,
                SubText = "Max / Min"
            });
        }

        if (climbSummary.MaxAvgSpeed is { } maxAvgSpeed &&
            climbSummary.MinAvgSpeed is { } minAvgSpeed)
        {
            items.Add(new ClimbSummaryKpi
            {
                Label = "Avg Speed",
                MaxText = FlightDetailsFormatters.FormatKmh(maxAvgSpeed.value),
                MaxClimbIndex = maxAvgSpeed.index,
                MinText = FlightDetailsFormatters.FormatKmh(minAvgSpeed.value),
                MinClimbIndex = minAvgSpeed.index,
                SubText = "Max / Min"
            });
        }

        if (climbSummary.MaxClimb is { } maxClimb &&
            climbSummary.MinClimb is { } minClimb)
        {
            items.Add(new ClimbSummaryKpi
            {
                Label = "Climb",
                MaxText = FlightDetailsFormatters.FormatMs(maxClimb.value),
                MaxClimbIndex = maxClimb.index,
                MinText = FlightDetailsFormatters.FormatMs(minClimb.value),
                MinClimbIndex = minClimb.index,
                SubText = "Max / Min"
            });
        }

        return items;
    }
}