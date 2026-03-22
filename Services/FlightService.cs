using FlightApp.Domain;
using Microsoft.AspNetCore.Components.Forms;

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

    public async Task ImportAndSaveAsync(IReadOnlyList<IBrowserFile>? files)
    {
        await _flightImportService.ImportAndSaveAsync(files);
    }

    public async Task DeleteFlightAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        await _flightStorage.DeleteFlightAsync(id);
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

        var track = _trackBinarySerializer.Deserialize(binary);
        if (track is null)
            return null;

        track.TimeSec = BuildTimeSec(track.TDeltaMs);

        // todo remove later
        const int repeatCount = 1;

        return RepeatTrack(track, repeatCount);
    }

    /// <summary>
    /// Builds cumulative relative flight time in whole seconds from per-fix delta times in milliseconds.
    /// The first track point is always 0 seconds.
    /// </summary>
    private static int[] BuildTimeSec(int[] tDeltaMs)
    {
        if (tDeltaMs.Length == 0)
            return Array.Empty<int>();

        var timeSec = new int[tDeltaMs.Length];
        long cumulativeMs = 0;

        timeSec[0] = 0;

        for (var i = 1; i < tDeltaMs.Length; i++)
        {
            cumulativeMs += tDeltaMs[i];
            timeSec[i] = (int)(cumulativeMs / 1000);
        }

        return timeSec;
    }

    private static TrackArrays RepeatTrack(TrackArrays source, int repeatCount)
    {
        if (repeatCount <= 1)
            return source;

        return new TrackArrays
        {
            TDeltaMs = RepeatArray(source.TDeltaMs, repeatCount),
            TimeSec = RepeatArray(source.TimeSec, repeatCount),
            LatE7 = RepeatArray(source.LatE7, repeatCount),
            LonE7 = RepeatArray(source.LonE7, repeatCount),
            AltBaroCm = RepeatNullableArray(source.AltBaroCm, repeatCount),
            AltGpsCm = RepeatNullableArray(source.AltGpsCm, repeatCount),
            VarioCms = RepeatNullableArray(source.VarioCms, repeatCount),
            SpeedCms = RepeatNullableArray(source.SpeedCms, repeatCount)
        };
    }

    private static int[] RepeatArray(int[] source, int repeatCount)
    {
        if (source.Length == 0 || repeatCount <= 1)
            return source;

        var result = new int[source.Length * repeatCount];

        for (var r = 0; r < repeatCount; r++)
        {
            Array.Copy(source, 0, result, r * source.Length, source.Length);
        }

        return result;
    }

    private static int[]? RepeatNullableArray(int[]? source, int repeatCount)
    {
        if (source is null)
            return null;

        return RepeatArray(source, repeatCount);
    }
}