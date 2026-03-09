using FlightApp.Domain;
using Microsoft.JSInterop;

namespace FlightApp.Services;

public class IndexedDbFlightStorage : IFlightStorage
{
    private readonly IJSRuntime _js;

    public IndexedDbFlightStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SaveFlightAsync(Flight flight)
    {
        await _js.InvokeVoidAsync("flightDb.putFlight", flight);
    }

    public async Task<List<Flight>> GetFlightsAsync()
    {
        var flights = await _js.InvokeAsync<List<Flight>?>("flightDb.getFlights");
        return flights ?? new List<Flight>();
    }

    public async Task<Flight?> GetFlightByIdAsync(string id)
    {
        return await _js.InvokeAsync<Flight?>("flightDb.getFlightById", id);
    }

    public async Task SaveTrackAsync(string flightId, byte[] trackBinary)
    {
        await _js.InvokeVoidAsync("flightDb.putTrack", flightId, trackBinary);
    }

    public async Task<byte[]?> GetTrackAsync(string flightId)
    {
        return await _js.InvokeAsync<byte[]?>("flightDb.getTrack", flightId);
    }

    public async Task SaveIgcAsync(string flightId, string igcContent)
    {
        await _js.InvokeVoidAsync("flightDb.putIgc", flightId, igcContent);
    }

    public async Task<string?> GetIgcAsync(string flightId)
    {
        return await _js.InvokeAsync<string?>("flightDb.getIgc", flightId);
    }
}