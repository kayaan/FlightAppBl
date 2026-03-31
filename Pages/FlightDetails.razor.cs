namespace FlightApp.Pages;

using FlightApp.Analysis;
using FlightApp.Components;
using FlightApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

public partial class FlightDetails
{
    [Parameter]
    public string? Id { get; set; }

    private DotNetObjectReference<FlightDetails>? dotNetRef;

    private ClimbSegment? HoveredClimb => State.HoveredClimb;
    private ClimbSegment? SelectedClimb => State.SelectedClimb;

    private bool CanSelectPreviousClimb => State.CanSelectPreviousClimb;
    private bool CanSelectNextClimb => State.CanSelectNextClimb;
    private bool CanClear => State.CanClear;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        dotNetRef ??= DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("flightCharts.registerSelectionCallback", dotNetRef);
    }

    [JSInvokable]
    public Task OnChartSelection(int startIndex, int endIndex)
    {
        State.ClearSelectedClimb();
        ApplySelection(startIndex, endIndex);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnChartSelectionCleared()
    {
        ClearSelection();
        return Task.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        State.ResetForLoad();

        try
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                State.SetError("Missing flight id.");
                return;
            }

            var result = await FlightDetailsLoadService.LoadAsync(Id);

            State.SetLoadResult(
                result.Flight,
                result.TrackArrays,
                result.DisplayedStats,
                result.ClimbSegments,
                result.ClimbSummary,
                result.AllClimbKpis);
        }
        catch (Exception ex)
        {
            State.SetError(ex.Message);
        }
        finally
        {
            State.SetLoadingFinished();
        }
    }

    private void SetHoveredClimb(int index)
    {
        State.SetHoveredClimb(index);
    }

    private void ClearHoveredClimb()
    {
        State.ClearHoveredClimb();
    }

    private void ApplySelection(int startIndex, int endIndex)
    {
        if (State.TrackArrays is null)
            return;

        var result = SelectionService.ApplySelection(
            State.TrackArrays,
            startIndex,
            endIndex);

        State.ApplySelectionResult(result);
    }

    private void ClearSelection()
    {
        var result = SelectionService.ClearSelection(State.Flight);
        State.ApplySelectionResult(result);
    }

    private void SelectPreviousClimb()
    {
        if (State.TrackArrays is null)
            return;

        var result = SelectionService.SelectPreviousClimb(
            State.TrackArrays,
            State.ClimbSegments,
            State.SelectedClimbIndex);

        State.ApplySelectionResult(result);
    }

    private void SelectNextClimb()
    {
        if (State.TrackArrays is null)
            return;

        var result = SelectionService.SelectNextClimb(
            State.TrackArrays,
            State.ClimbSegments,
            State.SelectedClimbIndex);

        State.ApplySelectionResult(result);
    }

    private void SelectClimb(int index)
    {
        if (State.TrackArrays is null)
            return;

        State.ClearHoverOnSelection();

        var result = SelectionService.SelectClimb(
            State.TrackArrays,
            State.ClimbSegments,
            index);

        State.ApplySelectionResult(result);
    }

    private string GetLeftPaneStyle()
        => $"flex: 0 0 {State.LeftPanePercent}%";

    private async Task StartResize(MouseEventArgs _)
    {
        dotNetRef ??= DotNetObjectReference.Create(this);
        State.SetIsResizing(true);

        await JS.InvokeVoidAsync("flightDetails.startResize", State.FlightLayoutRef, dotNetRef);
    }

    [JSInvokable]
    public Task SetResizeBounds(double left, double width)
    {
        State.LayoutLeft = left;
        State.LayoutWidth = width;
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnResizeDrag(double clientX)
    {
        if (State.LayoutWidth <= 0)
            return;

        var relativeX = clientX - State.LayoutLeft;
        var newPercent = (int)Math.Round(relativeX / State.LayoutWidth * 100.0);
        var clamped = Math.Clamp(newPercent, 30, 75);

        if (!State.SetLeftPanePercent(clamped))
            return;

        await JS.InvokeVoidAsync("flightDetails.notifyResize");
    }

    [JSInvokable]
    public async Task OnResizeEnd()
    {
        State.SetIsResizing(false);
        await JS.InvokeVoidAsync("flightDetails.notifyResize");
    }

    private string GetClimbNavigatorTitle()
    {
        if (State.ClimbSegments.Count == 0)
            return "No climbs";

        if (!State.SelectedClimbIndex.HasValue)
            return $"Climb — / {State.ClimbSegments.Count}";

        return $"Climb {State.SelectedClimbIndex.Value + 1} / {State.ClimbSegments.Count}";
    }

    private string GetClimbNavigatorSubtitle()
    {
        if (State.ClimbSegments.Count == 0)
            return string.Empty;

        var climb = SelectedClimb;
        if (climb is null)
            return $"{State.ClimbSegments.Count} climbs detected";

        return $"{FlightDetailsFormatters.FormatGain(climb)} · {climb.AvgClimbRateMs:0.0} m/s";
    }

    private void ToggleShowAllClimbs()
    {
        State.ToggleShowAllClimbs();
    }

    public override async ValueTask DisposeAsync()
    {
        if (dotNetRef is not null)
        {
            dotNetRef.Dispose();
            dotNetRef = null;
        }

        try
        {
            await JS.InvokeVoidAsync("flightCharts.clearSelectionCallback");
        }
        catch
        {
        }

        await base.DisposeAsync();
    }
}