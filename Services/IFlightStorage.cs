using FlightApp.Domain;

namespace FlightApp.Services;

public interface IFlightStorage
{
    Task SaveFlightAggregateAsync(Flight flight, byte[] trackBinary, string igcContent);

    Task<List<Flight>> GetFlightsAsync();
    Task<Flight?> GetFlightByIdAsync(string id);
    Task DeleteFlightAsync(string id);

    Task<byte[]?> GetTrackAsync(string flightId);

    Task<string?> GetIgcAsync(string flightId);

    Task<Flight?> GetFlightByFileHashAsync(string fileHash);
}