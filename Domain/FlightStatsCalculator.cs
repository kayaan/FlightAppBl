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

        double totalClimbMeters = 0;
        double totalSinkMeters = 0;

        double totalVario = 0;
        int varioCount = 0;
        double? maxVario = null;
        double? minVario = null;

        FixPoint? previous = null;
        int varioWindowStartIndex = 0;

        for (int i = 0; i < fixes.Count; i++)
        {
            var fix = fixes[i];

            UpdateAltitudeStats(
                fix,
                ref gpsMin,
                ref gpsMax,
                ref baroMin,
                ref baroMax);

            UpdateSegmentStats(
                fix,
                previous,
                ref totalDistance,
                ref totalTimeSeconds,
                ref maxSpeed,
                ref totalClimbMeters,
                ref totalSinkMeters);

            UpdateVarioStats(
                fixes,
                i,
                ref varioWindowStartIndex,
                ref totalVario,
                ref varioCount,
                ref maxVario,
                ref minVario);

            previous = fix;
        }

        FinalizeStats(
            stats,
            fixes,
            first,
            last,
            gpsMin,
            gpsMax,
            baroMin,
            baroMax,
            totalDistance,
            totalTimeSeconds,
            maxSpeed,
            totalClimbMeters,
            totalSinkMeters,
            totalVario,
            varioCount,
            maxVario,
            minVario);

        return stats;
    }

    private void UpdateAltitudeStats(
        FixPoint fix,
        ref int? gpsMin,
        ref int? gpsMax,
        ref int? baroMin,
        ref int? baroMax)
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
    }

    private void UpdateSegmentStats(
        FixPoint fix,
        FixPoint? previous,
        ref double totalDistance,
        ref double totalTimeSeconds,
        ref double? maxSpeed,
        ref double totalClimbMeters,
        ref double totalSinkMeters)
    {
        if (previous == null)
            return;

        var distance = HaversineMeters(
            previous.Latitude,
            previous.Longitude,
            fix.Latitude,
            fix.Longitude);

        var deltaTime = (fix.TimeUtc - previous.TimeUtc).TotalSeconds;

        if (deltaTime <= 0)
            return;

        totalDistance += distance;
        totalTimeSeconds += deltaTime;

        var speedKmh = (distance / deltaTime) * 3.6;

        if (maxSpeed == null || speedKmh > maxSpeed)
            maxSpeed = speedKmh;

        double? prevAlt = previous.AltitudeBaro ?? previous.AltitudeGps;
        double? currAlt = fix.AltitudeBaro ?? fix.AltitudeGps;

        if (prevAlt.HasValue && currAlt.HasValue)
        {
            var deltaAlt = currAlt.Value - prevAlt.Value;

            if (deltaAlt > 0)
                totalClimbMeters += deltaAlt;
            else if (deltaAlt < 0)
                totalSinkMeters += -deltaAlt;
        }
    }

    private void UpdateVarioStats(
        List<FixPoint> fixes,
        int i,
        ref int windowIndex,
        ref double totalVario,
        ref int varioCount,
        ref double? maxVario,
        ref double? minVario)
    {
        var fix = fixes[i];

        while (windowIndex < i &&
               (fix.TimeUtc - fixes[windowIndex].TimeUtc).TotalSeconds >
               FlightConstants.DefaultVarioWindowSeconds)
        {
            windowIndex++;
        }

        if (windowIndex >= i)
            return;

        var baseFix = fixes[windowIndex];

        var deltaTime = (fix.TimeUtc - baseFix.TimeUtc).TotalSeconds;

        if (deltaTime <= 0)
            return;

        double? baseAlt = baseFix.AltitudeBaro ?? baseFix.AltitudeGps;
        double? currentAlt = fix.AltitudeBaro ?? fix.AltitudeGps;

        if (!baseAlt.HasValue || !currentAlt.HasValue)
            return;

        var vario = (currentAlt.Value - baseAlt.Value) / deltaTime;

        totalVario += vario;
        varioCount++;

        if (maxVario == null || vario > maxVario)
            maxVario = vario;

        if (minVario == null || vario < minVario)
            minVario = vario;
    }

    private void FinalizeStats(
        FlightStats stats,
        List<FixPoint> fixes,
        FixPoint first,
        FixPoint last,
        int? gpsMin,
        int? gpsMax,
        int? baroMin,
        int? baroMax,
        double totalDistance,
        double totalTimeSeconds,
        double? maxSpeed,
        double totalClimbMeters,
        double totalSinkMeters,
        double totalVario,
        int varioCount,
        double? maxVario,
        double? minVario)
    {
        stats.TotalDistanceMeters = totalDistance;

        if (totalTimeSeconds > 0)
            stats.AvgGroundSpeedKmh = (totalDistance / totalTimeSeconds) * 3.6;

        stats.MaxGroundSpeedKmh = maxSpeed;

        if (varioCount > 0)
            stats.AvgVarioMs = totalVario / varioCount;

        stats.MaxVarioMs = maxVario;
        stats.MinVarioMs = minVario;

        stats.TotalClimbMeters = totalClimbMeters;
        stats.TotalSinkMeters = totalSinkMeters;

        stats.AltGpsStart = first.AltitudeGps;
        stats.AltGpsEnd = last.AltitudeGps;
        stats.AltGpsMin = gpsMin;
        stats.AltGpsMax = gpsMax;

        stats.AltBaroStart = first.AltitudeBaro;
        stats.AltBaroEnd = last.AltitudeBaro;
        stats.AltBaroMin = baroMin;
        stats.AltBaroMax = baroMax;
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