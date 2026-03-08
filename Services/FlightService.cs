using FlightApp.Domain;

namespace FlightApp.Services;

public class FlightService
{
    private readonly FlightImportService _flightImportService;
    private readonly IFlightStorage _flightStorage;

    public FlightService(
        FlightImportService flightImportService,
        IFlightStorage flightStorage)
    {
        _flightImportService = flightImportService;
        _flightStorage = flightStorage;
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
}