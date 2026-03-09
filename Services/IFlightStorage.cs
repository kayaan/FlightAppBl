using FlightApp.Domain;

namespace FlightApp.Services;

public interface IFlightStorage
{
    Task SaveFlightAsync(Flight flight);
    Task<List<Flight>> GetFlightsAsync();
    Task<Flight?> GetFlightByIdAsync(string id);

    Task SaveTrackAsync(string flightId, byte[] trackBinary);
    Task<byte[]?> GetTrackAsync(string flightId);

    Task SaveIgcAsync(string flightId, string igcContent);
    Task<string?> GetIgcAsync(string flightId);
}