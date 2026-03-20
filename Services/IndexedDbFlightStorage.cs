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

    public async Task DeleteFlightAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        await _js.InvokeVoidAsync("flightDb.deleteFlight", id);
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

    public async Task<byte[]?> GetTrackAsync(string flightId)
    {
        return await _js.InvokeAsync<byte[]?>("flightDb.getTrack", flightId);
    }

    public async Task<string?> GetIgcAsync(string flightId)
    {
        return await _js.InvokeAsync<string?>("flightDb.getIgc", flightId);
    }

    public async Task SaveFlightAggregateAsync(Flight flight, byte[] trackBinary, string igcContent)
    {
        await _js.InvokeVoidAsync("flightDb.saveFlightAggregate", flight, trackBinary, igcContent);
    }

    public async Task<Flight?> GetFlightByFileHashAsync(string fileHash)
    {
        return await _js.InvokeAsync<Flight?>("flightDb.getFlightByFileHashAsync", fileHash);
    }

}