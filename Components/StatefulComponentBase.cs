// Components\StatefulComponentBase.cs

namespace  FlightApp.Components;

using FlightApp.Services;
using Microsoft.AspNetCore.Components;

public abstract class StatefulComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected FlightDetailsStateService State { get; set; } = default!;

    protected override void OnInitialized()
    {
        State.Changed += OnStateChanged;
    }

    private void OnStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public virtual ValueTask DisposeAsync()
    {
        State.Changed -= OnStateChanged;
        return ValueTask.CompletedTask;
    }
}