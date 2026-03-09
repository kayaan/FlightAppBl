using FlightApp.Analysis;
using FlightApp.Domain;

namespace FlightApp.Services;

public class FlightImportService
{
    private readonly IgcParser _parser;
    private readonly FlightStatsCalculator _statsCalculator;
    private readonly TrackBinarySerializer _trackBinarySerializer;
    private readonly IFlightStorage _flightStorage;

    public FlightImportService(
        IgcParser parser,
        FlightStatsCalculator statsCalculator,
        TrackBinarySerializer trackBinarySerializer,
        IFlightStorage flightStorage)
    {
        _parser = parser;
        _statsCalculator = statsCalculator;
        _trackBinarySerializer = trackBinarySerializer;
        _flightStorage = flightStorage;
    }

    public async Task<Flight> ImportAndSaveAsync(string igcContent)
    {
        var result = ImportInternal(igcContent);

        var trackBinary = _trackBinarySerializer.Serialize(result.Track);

        await _flightStorage.SaveFlightAsync(result.Flight);
        await _flightStorage.SaveTrackAsync(result.Flight.Id, trackBinary);
        await _flightStorage.SaveIgcAsync(result.Flight.Id, igcContent);

        return result.Flight;
    }

    private FlightImportResult ImportInternal(string igcContent)
    {
        var header = _parser.ParseHeader(igcContent);
        var fixes = _parser.ParseFixes(igcContent);

        var stats = _statsCalculator.Calculate(fixes);
        var track = BuildTrackArrays(fixes);

        var flight = new Flight
        {
            Date = header!.Date!.ToString(),
            PilotId = header.Pilot,
            GliderId = header.Glider,
            Stats = stats
        };

        return new FlightImportResult
        {
            Flight = flight,
            Track = track
        };
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