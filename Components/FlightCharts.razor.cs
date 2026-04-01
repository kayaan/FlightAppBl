// Components\FlightCharts.razor.cs

using Microsoft.JSInterop;

namespace FlightApp.Components;

public partial class FlightCharts
{
    private string? lastHoveredClimbSignature;

    private readonly string altChartId = $"chart-alt-{Guid.NewGuid():N}";
    private readonly string varioChartId = $"chart-vario-{Guid.NewGuid():N}";
    private readonly string speedChartId = $"chart-speed-{Guid.NewGuid():N}";

    private bool disposed;

    private string? lastDataSignature;
    private string? lastClimbSignature;
    private string? lastAllClimbsSignature;

    private bool HasTrack => State.TrackArrays?.TDeltaMs is { Length: > 1 };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (disposed)
            return;

        if (!HasTrack)
        {
            if (lastDataSignature is not null)
            {
                await JS.InvokeVoidAsync("flightCharts.dispose", altChartId);
                await JS.InvokeVoidAsync("flightCharts.dispose", varioChartId);
                await JS.InvokeVoidAsync("flightCharts.dispose", speedChartId);

                lastDataSignature = null;
                lastClimbSignature = null;
                lastAllClimbsSignature = null;
                lastHoveredClimbSignature = null;
            }

            return;
        }

        var dataSignature = BuildDataSignature();

        if (dataSignature != lastDataSignature)
        {
            var payload = BuildBasePayload();
            if (payload is null)
                return;

            await JS.InvokeVoidAsync(
                "flightCharts.renderAll",
                altChartId,
                varioChartId,
                speedChartId,
                payload);

            lastDataSignature = dataSignature;
            lastClimbSignature = null;
            lastAllClimbsSignature = null;
            lastHoveredClimbSignature = null;
        }

        var allSignature = BuildAllClimbsSignature();

        if (allSignature != lastAllClimbsSignature)
        {
            var payload = BuildAllClimbsPayload();

            await JS.InvokeVoidAsync(
                "flightCharts.updateAllClimbs",
                altChartId,
                varioChartId,
                speedChartId,
                payload);

            lastAllClimbsSignature = allSignature;
        }

        var hoveredSignature = BuildHoveredClimbSignature();

        if (hoveredSignature != lastHoveredClimbSignature)
        {
            var payload = BuildHoveredClimbPayload();

            await JS.InvokeVoidAsync(
                "flightCharts.updateHoveredClimb",
                altChartId,
                varioChartId,
                speedChartId,
                payload);

            lastHoveredClimbSignature = hoveredSignature;
        }

        var climbSignature = BuildClimbSignature();

        if (climbSignature != lastClimbSignature)
        {
            var payload = BuildSelectedClimbPayload();

            await JS.InvokeVoidAsync(
                "flightCharts.updateSelectedClimb",
                altChartId,
                varioChartId,
                speedChartId,
                payload);

            lastClimbSignature = climbSignature;
        }
    }

    private string BuildHoveredClimbSignature()
    {
        if (!State.ShowAllClimbs)
            return "off";

        var begin = State.HoveredClimb?.BeginIndex ?? -1;
        var end = State.HoveredClimb?.EndIndex ?? -1;

        return $"{begin}:{end}";
    }

    private HoveredClimbPayload BuildHoveredClimbPayload()
    {
        if (!State.ShowAllClimbs ||
            State.TrackArrays?.TimeSec is not { Length: > 0 } timeSec ||
            State.HoveredClimb is null ||
            State.ClimbSegments is null)
        {
            return new HoveredClimbPayload();
        }

        var hoveredIndex = -1;

        for (int i = 0; i < State.ClimbSegments.Count; i++)
        {
            var climb = State.ClimbSegments[i];

            if (climb.BeginIndex == State.HoveredClimb.BeginIndex &&
                climb.EndIndex == State.HoveredClimb.EndIndex)
            {
                hoveredIndex = i;
                break;
            }
        }

        return new HoveredClimbPayload
        {
            HoveredClimbBeginSec = timeSec[State.HoveredClimb.BeginIndex],
            HoveredClimbEndSec = timeSec[State.HoveredClimb.EndIndex],
            HoveredClimbIndex = hoveredIndex >= 0 ? hoveredIndex : null
        };
    }

    private string BuildDataSignature()
    {
        var t = State.TrackArrays!.TDeltaMs;
        var count = t.Length;

        var alt = SelectAltitudeSource();
        var speed = State.TrackArrays.SpeedCms;
        var vario = State.TrackArrays.VarioCms;

        return $"{count}:{alt?.Length ?? 0}:{speed?.Length ?? 0}:{vario?.Length ?? 0}:{t[0]}:{t[count - 1]}";
    }

    private string BuildClimbSignature()
    {
        var begin = State.SelectedClimb?.BeginIndex ?? -1;
        var end = State.SelectedClimb?.EndIndex ?? -1;

        return $"{begin}:{end}";
    }

    private string BuildAllClimbsSignature()
    {
        var showAll = State.ShowAllClimbs;
        var count = State.ClimbSegments?.Count ?? 0;
        var selected = State.SelectedClimbIndex ?? -1;
        var hovered = State.HoveredClimbIndex ?? -1;

        return $"{showAll}:{count}:{selected}:{hovered}";
    }

    private FlightChartsPayload? BuildBasePayload()
    {
        if (State.TrackArrays?.TDeltaMs is not { Length: > 1 } tDeltaMs)
            return null;

        return new FlightChartsPayload
        {
            TimeSec = BuildTimeSeconds(tDeltaMs),

            AltitudeTitle = "Altitude",
            AltitudeUnit = "m",
            AltitudeValues = BuildScaledSeries(SelectAltitudeSource(), 0.01),

            VarioTitle = "Vario",
            VarioUnit = "m/s",
            VarioValues = BuildScaledSeries(State.TrackArrays.VarioCms, 0.01),

            SpeedTitle = "Speed",
            SpeedUnit = "km/h",
            SpeedValues = BuildScaledSeries(State.TrackArrays.SpeedCms, 0.036)
        };
    }

    private SelectedClimbPayload BuildSelectedClimbPayload()
    {
        return new SelectedClimbPayload
        {
            SelectedClimbBeginIndex = State.SelectedClimb?.BeginIndex,
            SelectedClimbEndIndex = State.SelectedClimb?.EndIndex
        };
    }

    private AllClimbsPayload BuildAllClimbsPayload()
    {
        if (!State.ShowAllClimbs || State.ClimbSegments == null)
            return new AllClimbsPayload { ShowAllClimbs = false };

        return new AllClimbsPayload
        {
            ShowAllClimbs = true,
            Begin = State.ClimbSegments.Select(c => c.BeginIndex).ToList(),
            End = State.ClimbSegments.Select(c => c.EndIndex).ToList()
        };
    }

    private static double[] BuildTimeSeconds(int[] tDeltaMs)
    {
        var result = new double[tDeltaMs.Length];
        double total = 0;

        for (int i = 0; i < tDeltaMs.Length; i++)
        {
            total += tDeltaMs[i];
            result[i] = total / 1000.0;
        }

        return result;
    }

    private static double[]? BuildScaledSeries(int[]? source, double scale)
    {
        if (source is null || source.Length == 0)
            return null;

        var result = new double[source.Length];

        for (int i = 0; i < source.Length; i++)
            result[i] = source[i] * scale;

        return result;
    }

    private int[]? SelectAltitudeSource()
    {
        if (State.TrackArrays?.AltBaroCm is { Length: > 0 })
            return State.TrackArrays.AltBaroCm;

        if (State.TrackArrays?.AltGpsCm is { Length: > 0 })
            return State.TrackArrays.AltGpsCm;

        return null;
    }

    public override async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;

        try
        {
            await JS.InvokeVoidAsync("flightCharts.dispose", altChartId);
            await JS.InvokeVoidAsync("flightCharts.dispose", varioChartId);
            await JS.InvokeVoidAsync("flightCharts.dispose", speedChartId);
        }
        catch
        {
        }

        await base.DisposeAsync();
    }
}