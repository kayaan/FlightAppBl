using FlightApp.Domain;

namespace FlightApp.Analysis;

public class FlightStatsCalculator
{
    public FlightStats Calculate(List<FixPoint> fixes)
    {
        var stats = new FlightStats();

        if (fixes == null || fixes.Count == 0)
            return stats;

        stats.FixCount = fixes.Count;

        // Start / End
        var start = fixes[0].TimeUtc;
        var end = fixes[^1].TimeUtc;

        stats.StartTimeUtc = start;
        stats.EndTimeUtc = end;

        stats.Duration = end - start;

        // Altitude Stats
        var gpsAltitudes = fixes
            .Where(f => f.AltitudeGps.HasValue)
            .Select(f => f.AltitudeGps!.Value)
            .ToList();

        if (gpsAltitudes.Count > 0)
        {
            stats.AltGpsMin = gpsAltitudes.Min();
            stats.AltGpsMax = gpsAltitudes.Max();
            stats.AltGpsStart = fixes.First().AltitudeGps;
            stats.AltGpsEnd = fixes.Last().AltitudeGps;
        }

        var baroAltitudes = fixes
            .Where(f => f.AltitudeBaro.HasValue)
            .Select(f => f.AltitudeBaro!.Value)
            .ToList();

        if (baroAltitudes.Count > 0)
        {
            stats.AltBaroMin = baroAltitudes.Min();
            stats.AltBaroMax = baroAltitudes.Max();
            stats.AltBaroStart = fixes.First().AltitudeBaro;
            stats.AltBaroEnd = fixes.Last().AltitudeBaro;
        }

        return stats;
    }
}