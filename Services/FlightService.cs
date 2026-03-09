using FlightApp.Domain;

namespace FlightApp.Services;

public class FlightService
{
    private readonly FlightImportService _flightImportService;
    private readonly IFlightStorage _flightStorage;
    private readonly TrackBinarySerializer _trackBinarySerializer;

    public FlightService(
        FlightImportService flightImportService,
        IFlightStorage flightStorage,
        TrackBinarySerializer trackBinarySerializer)
    {
        _flightImportService = flightImportService;
        _flightStorage = flightStorage;
        _trackBinarySerializer = trackBinarySerializer;
    }

    public async Task ImportFilesAsync(IEnumerable<string> igcContents)
    {
        foreach (var content in igcContents)
        {
            await _flightImportService.ImportAndSaveAsync(content);
        }
    }

    public Task<List<Flight>> GetFlightsAsync()
    {
        return _flightStorage.GetFlightsAsync();
    }

    public Task<Flight?> GetFlightByIdAsync(string id)
    {
        return _flightStorage.GetFlightByIdAsync(id);
    }

    public async Task<TrackArrays?> GetTrackAsync(string flightId)
    {
        var binary = await _flightStorage.GetTrackAsync(flightId);
        if (binary is null)
            return null;

        return _trackBinarySerializer.Deserialize(binary);
    }
}