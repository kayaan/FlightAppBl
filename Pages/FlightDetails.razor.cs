// Pages\FlightDetails.razor.cs
namespace FlightApp.Pages;


using FlightApp.Components;
using Microsoft.JSInterop;


public partial class FlightDetails
{
    private DotNetObjectReference<FlightDetails>? dotNetRef;

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