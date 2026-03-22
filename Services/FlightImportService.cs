using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FlightApp.Analysis;
using FlightApp.Domain;
using Microsoft.AspNetCore.Components.Forms;


namespace FlightApp.Services;

public class FlightImportService
{
    private readonly IgcParser _parser;

    private readonly FlightStatsCalculator _statsCalculator;

    private readonly TrackBinarySerializer _trackBinarySerializer;

    private readonly IFlightStorage _flightStorage;

    private ToastService _toastService;

    private FlightImportStateService _importState;

    public string? CurrentFileName { get; private set; }

    public FlightImportService(
        IgcParser parser,
        FlightStatsCalculator statsCalculator,
        TrackBinarySerializer trackBinarySerializer,
        IFlightStorage flightStorage,
        ToastService toastService,
        FlightImportStateService importState)
    {
        _parser = parser;
        _statsCalculator = statsCalculator;
        _trackBinarySerializer = trackBinarySerializer;
        _flightStorage = flightStorage;
        _toastService = toastService;
        _importState = importState;
    }

    public async Task ImportAndSaveAsync(IReadOnlyList<IBrowserFile>? files)
    {
        if (files == null || files.Count == 0)
        {
            _toastService.Show("No file selected.", ToastType.Info);
            return;
        }

        _importState.Start(files.Count);

        try
        {
            foreach (var file in files)
            {
                _importState.SetCurrentFile(file.Name, "Reading file...");
                await Task.Yield();

                try
                {
                    if (!file.Name.EndsWith(".igc", StringComparison.OrdinalIgnoreCase))
                    {
                        _toastService.Show($"{file.Name}: Only .igc files are allowed.", ToastType.Error);
                        continue;
                    }

                    if (file.Size <= 0)
                    {
                        _toastService.Show($"{file.Name}: File is empty.", ToastType.Error);
                        continue;
                    }

                    using var stream = file.OpenReadStream(10 * 1024 * 1024);
                    using var reader = new StreamReader(stream);

                    var content = await reader.ReadToEndAsync();

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _toastService.Show($"{file.Name}: File is empty.", ToastType.Error);
                        continue;
                    }

                    _importState.SetMessage("Checking duplicate...");

                    var hash = ComputeHash(content);
                    var existing = await _flightStorage.GetFlightByFileHashAsync(hash);

                    if (existing is not null)
                    {
                        _toastService.Show($"{file.Name}: Flight already exists.", ToastType.Info);
                        continue;
                    }

                    _importState.SetMessage("Importing flight...");

                    var result = ImportInternal(content);
                    result.Flight.FileHash = hash;

                    _importState.SetMessage("Saving flight...");

                    var trackBinary = _trackBinarySerializer.Serialize(result.Track);

                    await _flightStorage.SaveFlightAggregateAsync(
                        result.Flight,
                        trackBinary,
                        content
                    );

                    _toastService.Show($"{file.Name}: Flight imported.", ToastType.Success);
                }
                catch
                {
                    _toastService.Show($"{file.Name}: Import failed.", ToastType.Error);
                }
                finally
                {
                    _importState.Advance();
                    await Task.Yield();
                }
            }
        }
        finally
        {
            _importState.Finish();
        }
    }
    private static string ComputeHash(string content)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha.ComputeHash(bytes);

        return Convert.ToHexString(hash);
    }

    private FlightImportResult ImportInternal(string igcContent)
    {
        var header = _parser.ParseHeader(igcContent);
        var fixes = _parser.ParseFixes(igcContent);

        var stats = _statsCalculator.Calculate(fixes);
        var track = BuildTrackArrays(fixes);

        var flight = new Flight
        {
            Date = FormatFlightDate(header?.Date),
            PilotId = header?.Pilot,
            GliderId = header?.Glider,
            Stats = stats
        };

        return new FlightImportResult
        {
            Flight = flight,
            Track = track
        };
    }

    private static string? FormatFlightDate(object? date)
    {
        if (date is null)
            return null;

        if (date is string s)
            return s;

        if (date is DateTime dt)
            return dt.ToString("yyyy-MM-dd");

        var type = date.GetType();

        var yearProp = type.GetProperty("Year", BindingFlags.Public | BindingFlags.Instance);
        var monthProp = type.GetProperty("Month", BindingFlags.Public | BindingFlags.Instance);
        var dayProp = type.GetProperty("Day", BindingFlags.Public | BindingFlags.Instance);

        if (yearProp is not null && monthProp is not null && dayProp is not null)
        {
            var yearValue = yearProp.GetValue(date);
            var monthValue = monthProp.GetValue(date);
            var dayValue = dayProp.GetValue(date);

            if (yearValue is int year && monthValue is int month && dayValue is int day)
            {
                return $"{year:D4}-{month:D2}-{day:D2}";
            }
        }

        return date.ToString();
    }

    private TrackArrays BuildTrackArrays(List<FixPoint> fixes)
    {
        int n = fixes.Count;

        var tDeltaMs = new int[n];
        var latE7 = new int[n];
        var lonE7 = new int[n];
        var altGpsCm = new int[n];
        var altBaroCm = new int[n];
        var speedCms = new int[n];
        var varioCms = new int[n];

        int varioWindowStartIndex = 0;

        for (int i = 0; i < n; i++)
        {
            var f = fixes[i];

            tDeltaMs[i] = i == 0
                ? 0
                : (int)(f.TimeUtc - fixes[i - 1].TimeUtc).TotalMilliseconds;

            latE7[i] = (int)Math.Round(f.Latitude * 1e7);
            lonE7[i] = (int)Math.Round(f.Longitude * 1e7);

            altGpsCm[i] = (f.AltitudeGps ?? 0) * 100;
            altBaroCm[i] = (f.AltitudeBaro ?? 0) * 100;

            if (i == 0)
            {
                speedCms[i] = 0;
                varioCms[i] = 0;
                continue;
            }

            var prev = fixes[i - 1];
            var deltaTimeSec = (f.TimeUtc - prev.TimeUtc).TotalSeconds;

            if (deltaTimeSec > 0)
            {
                var distanceMeters = HaversineMeters(
                    prev.Latitude,
                    prev.Longitude,
                    f.Latitude,
                    f.Longitude);

                speedCms[i] = (int)Math.Round((distanceMeters / deltaTimeSec) * 100.0);
            }
            else
            {
                speedCms[i] = 0;
            }

            while (varioWindowStartIndex < i &&
                   (f.TimeUtc - fixes[varioWindowStartIndex].TimeUtc).TotalSeconds > FlightConstants.DefaultVarioWindowSeconds)
            {
                varioWindowStartIndex++;
            }

            if (varioWindowStartIndex < i)
            {
                var baseFix = fixes[varioWindowStartIndex];
                var varioDeltaTimeSec = (f.TimeUtc - baseFix.TimeUtc).TotalSeconds;

                if (varioDeltaTimeSec > 0)
                {
                    var baseAlt = baseFix.AltitudeBaro ?? baseFix.AltitudeGps;
                    var currentAlt = f.AltitudeBaro ?? f.AltitudeGps;

                    if (baseAlt.HasValue && currentAlt.HasValue)
                    {
                        var varioMs = (currentAlt.Value - baseAlt.Value) / varioDeltaTimeSec;
                        varioCms[i] = (int)Math.Round(varioMs * 100.0);
                    }
                    else
                    {
                        varioCms[i] = 0;
                    }
                }
                else
                {
                    varioCms[i] = 0;
                }
            }
            else
            {
                varioCms[i] = 0;
            }
        }

        return new TrackArrays
        {
            TDeltaMs = tDeltaMs,
            LatE7 = latE7,
            LonE7 = lonE7,
            AltGpsCm = altGpsCm,
            AltBaroCm = altBaroCm,
            SpeedCms = speedCms,
            VarioCms = varioCms
        };
    }

    private static double HaversineMeters(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
            Math.Cos(lat1) * Math.Cos(lat2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return FlightConstants.EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double deg)
    {
        return deg * Math.PI / 180.0;
    }
}