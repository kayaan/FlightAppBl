using FlightApp.Domain;

namespace FlightApp.Services;

public class InMemoryFlightStorage : IFlightStorage
{
    private readonly List<Flight> _flights = new();
    private readonly Dictionary<string, byte[]> _tracks = new();

    public Task SaveFlightAsync(Flight flight)
    {
        var existing = _flights.FirstOrDefault(f => f.Id == flight.Id);

        if (existing == null)
        {
            _flights.Add(flight);
        }
        else
        {
            var index = _flights.IndexOf(existing);
            _flights[index] = flight;
        }

        return Task.CompletedTask;
    }

    public Task<List<Flight>> GetFlightsAsync()
    {
        var result = _flights
            .OrderByDescending(f => f.Date)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<Flight?> GetFlightByIdAsync(string id)
    {
        var flight = _flights.FirstOrDefault(f => f.Id == id);
        return Task.FromResult(flight);
    }

    public Task SaveTrackAsync(string flightId, byte[] trackBinary)
    {
        _tracks[flightId] = trackBinary;
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetTrackAsync(string flightId)
    {
        _tracks.TryGetValue(flightId, out var track);
        return Task.FromResult(track);
    }
}