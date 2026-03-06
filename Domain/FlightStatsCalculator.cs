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

        var first = fixes[0];
        var last = fixes[^1];

        stats.StartTimeUtc = first.TimeUtc;
        stats.EndTimeUtc = last.TimeUtc;
        stats.Duration = last.TimeUtc - first.TimeUtc;

        int? gpsMin = null;
        int? gpsMax = null;

        int? baroMin = null;
        int? baroMax = null;

        double totalDistance = 0;
        double totalTimeSeconds = 0;
        double? maxSpeed = null;

        FixPoint? previous = null;

        foreach (var fix in fixes)
        {
            if (fix.AltitudeGps.HasValue)
            {
                var alt = fix.AltitudeGps.Value;

                if (gpsMin == null || alt < gpsMin)
                    gpsMin = alt;

                if (gpsMax == null || alt > gpsMax)
                    gpsMax = alt;
            }

            if (fix.AltitudeBaro.HasValue)
            {
                var alt = fix.AltitudeBaro.Value;

                if (baroMin == null || alt < baroMin)
                    baroMin = alt;

                if (baroMax == null || alt > baroMax)
                    baroMax = alt;
            }

            if (previous != null)
            {
                var distance = HaversineMeters(
                    previous.Latitude,
                    previous.Longitude,
                    fix.Latitude,
                    fix.Longitude
                );

                var deltaTime = (fix.TimeUtc - previous.TimeUtc).TotalSeconds;

                if (deltaTime > 0)
                {
                    totalDistance += distance;
                    totalTimeSeconds += deltaTime;

                    var speedMs = distance / deltaTime;
                    var speedKmh = speedMs * 3.6;

                    if (maxSpeed == null || speedKmh > maxSpeed)
                        maxSpeed = speedKmh;
                }
            }

            previous = fix;
        }

        stats.TotalDistanceMeters = totalDistance;

        if (totalTimeSeconds > 0)
        {
            stats.AvgGroundSpeedKmh = (totalDistance / totalTimeSeconds) * 3.6;
        }

        stats.MaxGroundSpeedKmh = maxSpeed;

        stats.AltGpsStart = first.AltitudeGps;
        stats.AltGpsEnd = last.AltitudeGps;
        stats.AltGpsMin = gpsMin;
        stats.AltGpsMax = gpsMax;

        stats.AltBaroStart = first.AltitudeBaro;
        stats.AltBaroEnd = last.AltitudeBaro;
        stats.AltBaroMin = baroMin;
        stats.AltBaroMax = baroMax;

        return stats;
    }

    private static double HaversineMeters(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double R = 6371000;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
            Math.Cos(lat1) * Math.Cos(lat2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double DegreesToRadians(double deg)
    {
        return deg * Math.PI / 180.0;
    }
}