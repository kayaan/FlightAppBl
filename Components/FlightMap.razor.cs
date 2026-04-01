// Components\FlightMap.razor.cs

using FlightApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FlightApp.Components;

/// <summary>
/// Code behind flight map
/// </summary>
public partial class FlightMap
{
    [Inject] private FlightDetailsSelectionService SelectionService { get; set; } = default!;

    [Parameter]
    public string Height { get; set; } = "100%";

    private DotNetObjectReference<FlightMap>? dotNetRef;

    private string? lastHoveredClimbSignature;
    private string? lastSelectedClimbSignature;

    private readonly string mapId = $"flight-map-{Guid.NewGuid():N}";
    private string? lastRenderSignature;
    private bool disposed;

    private bool HasTrackPoints =>
        State.TrackArrays?.LatE7 is { Length: > 0 } &&
        State.TrackArrays.LonE7 is { Length: > 0 };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (disposed)
            return;

        if (!HasTrackPoints)
        {
            if (lastRenderSignature is not null)
            {
                await JS.InvokeVoidAsync("flightMapComponent.clear", mapId);

                lastRenderSignature = null;
                lastHoveredClimbSignature = null;
                lastSelectedClimbSignature = null;
            }

            return;
        }

        var signature = BuildRenderSignature();

        if (signature != lastRenderSignature)
        {
            await JS.InvokeVoidAsync(
                "flightMapComponent.renderTrackArrays",
                mapId,
                State.TrackArrays!.LatE7,
                State.TrackArrays.LonE7,
                State.TrackArrays.VarioCms,
                State.MapStyle.ToString().ToLowerInvariant());

            lastRenderSignature = signature;
            lastHoveredClimbSignature = null;
            lastSelectedClimbSignature = null;
        }

        var hoveredSignature = BuildHoveredClimbSignature();

        if (hoveredSignature != lastHoveredClimbSignature)
        {
            await JS.InvokeVoidAsync(
                "flightMapComponent.updateHoveredClimb",
                mapId,
                BuildHoveredClimbPayload());

            lastHoveredClimbSignature = hoveredSignature;
        }

        var selectedSignature = BuildSelectedClimbSignature();

        if (selectedSignature != lastSelectedClimbSignature)
        {
            await JS.InvokeVoidAsync(
                "flightMapComponent.updateSelectedClimb",
                mapId,
                BuildSelectedClimbPayload());

            lastSelectedClimbSignature = selectedSignature;
        }

        if (firstRender)
        {
            dotNetRef ??= DotNetObjectReference.Create(this);

            await JS.InvokeVoidAsync(
                "flightMapComponent.registerInteraction",
                mapId,
                dotNetRef);
        }
    }

    private HoveredClimbMapPayload BuildHoveredClimbPayload()
    {
        if (State.HoveredClimb is null || State.ClimbSegments is null)
            return new HoveredClimbMapPayload();

        var climbIndex = -1;

        for (int i = 0; i < State.ClimbSegments.Count; i++)
        {
            var climb = State.ClimbSegments[i];

            if (climb.BeginIndex == State.HoveredClimb.BeginIndex &&
                climb.EndIndex == State.HoveredClimb.EndIndex)
            {
                climbIndex = i;
                break;
            }
        }

        var isSameAsSelected =
            State.SelectedClimb is not null &&
            State.SelectedClimb.BeginIndex == State.HoveredClimb.BeginIndex &&
            State.SelectedClimb.EndIndex == State.HoveredClimb.EndIndex;

        return new HoveredClimbMapPayload
        {
            BeginIndex = State.HoveredClimb.BeginIndex,
            EndIndex = State.HoveredClimb.EndIndex,
            ClimbIndex = climbIndex >= 0 ? climbIndex : null,
            IsSameAsSelected = isSameAsSelected
        };
    }

    private SelectedClimbMapPayload BuildSelectedClimbPayload()
    {
        var begin = State.SelectedClimb?.BeginIndex;
        var end = State.SelectedClimb?.EndIndex;

        int? cursorIndex = null;

        if (begin.HasValue && end.HasValue)
        {
            cursorIndex = (begin.Value + end.Value) / 2;
        }

        return new SelectedClimbMapPayload
        {
            BeginIndex = begin,
            EndIndex = end,
            CursorIndex = cursorIndex
        };
    }

    private string BuildHoveredClimbSignature()
    {
        var begin = State.HoveredClimb?.BeginIndex ?? -1;
        var end = State.HoveredClimb?.EndIndex ?? -1;

        var sameAsSelected =
            State.HoveredClimb is not null &&
            State.SelectedClimb is not null &&
            State.HoveredClimb.BeginIndex == State.SelectedClimb.BeginIndex &&
            State.HoveredClimb.EndIndex == State.SelectedClimb.EndIndex;

        return $"{begin}:{end}:{sameAsSelected}";
    }

    private string BuildSelectedClimbSignature()
    {
        var begin = State.SelectedClimb?.BeginIndex ?? -1;
        var end = State.SelectedClimb?.EndIndex ?? -1;

        return $"{begin}:{end}";
    }

    private string BuildRenderSignature()
    {
        var latE7 = State.TrackArrays!.LatE7;
        var lonE7 = State.TrackArrays!.LonE7;

        var count = Math.Min(latE7.Length, lonE7.Length);

        if (count == 0)
            return $"0:{State.MapStyle}";

        return $"{State.MapStyle}:{count}:{latE7[0]}:{lonE7[0]}:{latE7[count - 1]}:{lonE7[count - 1]}";
    }

    [JSInvokable]
    public Task OnMapTrackHover(int trackIndex)
    {
        if (State.TrackArrays is null || State.ClimbSegments is null)
            return Task.CompletedTask;

        State.SetHoveredClimbFromTrackIndex(trackIndex);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnMapTrackLeave()
    {
        State.ClearHoveredClimb();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnMapTrackClick()
    {
        var hoveredIndex = State.HoveredClimbIndex;

        if (!hoveredIndex.HasValue || State.TrackArrays is null || State.ClimbSegments is null)
            return Task.CompletedTask;

        State.ClearHoverOnSelection();

        var result = SelectionService.SelectClimb(
            State.TrackArrays,
            State.ClimbSegments.ToList(),
            hoveredIndex.Value);

        State.ApplySelectionResult(result);

        return Task.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;

        if (dotNetRef is not null)
        {
            dotNetRef.Dispose();
            dotNetRef = null;
        }

        try
        {
            await JS.InvokeVoidAsync("flightMapComponent.clear", mapId);
        }
        catch
        {
        }

        await base.DisposeAsync();
    }
}