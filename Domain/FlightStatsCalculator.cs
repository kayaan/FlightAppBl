using FlightApp.Domain;

namespace FlightApp.Analysis;

public class FlightStatsCalculator
{
    public FlightStats Calculate(List<FixPoint> fixes)
    {
        var stats = new FlightStats();

        if (fixes == null || fixes.Count == 0)
            return stats;

        InitializeBasicStats(stats, fixes);

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

        int varioWindowStartIndex = 0;
        FixPoint? previous = null;

        for (int i = 0; i < fixes.Count; i++)
        {
            var fix = fixes[i];

            UpdateAltitudeMinMax(
                fix,
                ref gpsMin,
                ref gpsMax,
                ref baroMin,
                ref baroMax);

            UpdateSegmentDistanceSpeedAndClimbSink(
                previous,
                fix,
                ref totalDistance,
                ref totalTimeSeconds,
                ref maxSpeed,
                ref totalClimbMeters,
                ref totalSinkMeters);

            UpdateWindowVario(
                fixes,
                i,
                ref varioWindowStartIndex,
                ref totalVario,
                ref varioCount,
                ref maxVario,
                ref minVario);

            previous = fix;
        }

        ApplyAltitudeStats(stats, fixes[0], fixes[^1], gpsMin, gpsMax, baroMin, baroMax);
        ApplyDistanceAndSpeedStats(stats, totalDistance, totalTimeSeconds, maxSpeed);
        ApplyVarioStats(stats, totalVario, varioCount, maxVario, minVario);
        ApplyClimbSinkStats(stats, totalClimbMeters, totalSinkMeters);
        ApplyStartEndGainLossStats(stats, fixes[0], fixes[^1]);
        ApplyMaxAboveStartEndStats(stats, fixes[0], fixes[^1], gpsMax, baroMax);

        return stats;
    }

    private static void InitializeBasicStats(FlightStats stats, List<FixPoint> fixes)
    {
        var first = fixes[0];
        var last = fixes[^1];

        stats.FixCount = fixes.Count;
        stats.StartTimeUtc = first.TimeUtc;
        stats.EndTimeUtc = last.TimeUtc;
        stats.Duration = last.TimeUtc - first.TimeUtc;
    }

    private static void UpdateAltitudeMinMax(
        FixPoint fix,
        ref int? gpsMin,
        ref int? gpsMax,
        ref int? baroMin,
        ref int? baroMax)
    {
        UpdateMinMax(fix.AltitudeGps, ref gpsMin, ref gpsMax);
        UpdateMinMax(fix.AltitudeBaro, ref baroMin, ref baroMax);
    }

    private static void UpdateSegmentDistanceSpeedAndClimbSink(
        FixPoint? previous,
        FixPoint current,
        ref double totalDistance,
        ref double totalTimeSeconds,
        ref double? maxSpeed,
        ref double totalClimbMeters,
        ref double totalSinkMeters)
    {
        if (previous == null)
            return;

        var deltaTime = (current.TimeUtc - previous.TimeUtc).TotalSeconds;
        if (deltaTime <= 0)
            return;

        var distance = HaversineMeters(
            previous.Latitude,
            previous.Longitude,
            current.Latitude,
            current.Longitude);

        totalDistance += distance;
        totalTimeSeconds += deltaTime;

        var speedKmh = (distance / deltaTime) * 3.6;
        if (maxSpeed == null || speedKmh > maxSpeed.Value)
            maxSpeed = speedKmh;

        var prevAlt = GetPreferredAltitude(previous);
        var currAlt = GetPreferredAltitude(current);

        if (prevAlt.HasValue && currAlt.HasValue)
        {
            var deltaAlt = currAlt.Value - prevAlt.Value;

            if (deltaAlt > 0)
                totalClimbMeters += deltaAlt;
            else if (deltaAlt < 0)
                totalSinkMeters += -deltaAlt;
        }
    }

    private static void UpdateWindowVario(
        List<FixPoint> fixes,
        int currentIndex,
        ref int windowStartIndex,
        ref double totalVario,
        ref int varioCount,
        ref double? maxVario,
        ref double? minVario)
    {
        var current = fixes[currentIndex];

        while (windowStartIndex < currentIndex &&
               (current.TimeUtc - fixes[windowStartIndex].TimeUtc).TotalSeconds > FlightConstants.DefaultVarioWindowSeconds)
        {
            windowStartIndex++;
        }

        if (windowStartIndex >= currentIndex)
            return;

        var baseFix = fixes[windowStartIndex];
        var deltaTime = (current.TimeUtc - baseFix.TimeUtc).TotalSeconds;
        if (deltaTime <= 0)
            return;

        var baseAlt = GetPreferredAltitude(baseFix);
        var currentAlt = GetPreferredAltitude(current);

        if (!baseAlt.HasValue || !currentAlt.HasValue)
            return;

        var varioMs = (currentAlt.Value - baseAlt.Value) / deltaTime;

        totalVario += varioMs;
        varioCount++;

        if (maxVario == null || varioMs > maxVario.Value)
            maxVario = varioMs;

        if (minVario == null || varioMs < minVario.Value)
            minVario = varioMs;
    }

    private static void ApplyAltitudeStats(
        FlightStats stats,
        FixPoint first,
        FixPoint last,
        int? gpsMin,
        int? gpsMax,
        int? baroMin,
        int? baroMax)
    {
        stats.AltGpsStart = first.AltitudeGps;
        stats.AltGpsEnd = last.AltitudeGps;
        stats.AltGpsMin = gpsMin;
        stats.AltGpsMax = gpsMax;

        stats.AltBaroStart = first.AltitudeBaro;
        stats.AltBaroEnd = last.AltitudeBaro;
        stats.AltBaroMin = baroMin;
        stats.AltBaroMax = baroMax;
    }

    private static void ApplyDistanceAndSpeedStats(
        FlightStats stats,
        double totalDistance,
        double totalTimeSeconds,
        double? maxSpeed)
    {
        stats.TotalDistanceMeters = totalDistance;
        stats.MaxGroundSpeedKmh = maxSpeed;

        if (totalTimeSeconds > 0)
            stats.AvgGroundSpeedKmh = (totalDistance / totalTimeSeconds) * 3.6;
    }

    private static void ApplyVarioStats(
        FlightStats stats,
        double totalVario,
        int varioCount,
        double? maxVario,
        double? minVario)
    {
        if (varioCount > 0)
            stats.AvgVarioMs = totalVario / varioCount;

        stats.MaxVarioMs = maxVario;
        stats.MinVarioMs = minVario;
    }

    private static void ApplyClimbSinkStats(
        FlightStats stats,
        double totalClimbMeters,
        double totalSinkMeters)
    {
        stats.TotalClimbMeters = totalClimbMeters;
        stats.TotalSinkMeters = totalSinkMeters;
    }

    private static void ApplyStartEndGainLossStats(
        FlightStats stats,
        FixPoint first,
        FixPoint last)
    {
        ApplyGainLoss(first.AltitudeGps, last.AltitudeGps, out var gpsGain, out var gpsLoss);
        ApplyGainLoss(first.AltitudeBaro, last.AltitudeBaro, out var baroGain, out var baroLoss);

        stats.AltGpsGainMeters = gpsGain;
        stats.AltGpsLossMeters = gpsLoss;
        stats.AltBaroGainMeters = baroGain;
        stats.AltBaroLossMeters = baroLoss;
    }

    private static void ApplyMaxAboveStartEndStats(
        FlightStats stats,
        FixPoint first,
        FixPoint last,
        int? gpsMax,
        int? baroMax)
    {
        stats.MaxHeightAboveLaunchGps = CalculateDifference(gpsMax, first.AltitudeGps);
        stats.MaxHeightAboveLaunchBaro = CalculateDifference(baroMax, first.AltitudeBaro);

        stats.MaxHeightAboveLandingGps = CalculateDifference(gpsMax, last.AltitudeGps);
        stats.MaxHeightAboveLandingBaro = CalculateDifference(baroMax, last.AltitudeBaro);
    }

    private static void UpdateMinMax(int? value, ref int? min, ref int? max)
    {
        if (!value.HasValue)
            return;

        if (min == null || value.Value < min.Value)
            min = value.Value;

        if (max == null || value.Value > max.Value)
            max = value.Value;
    }

    private static double? GetPreferredAltitude(FixPoint fix)
    {
        return fix.AltitudeBaro ?? fix.AltitudeGps;
    }

    private static void ApplyGainLoss(int? start, int? end, out int? gain, out int? loss)
    {
        gain = null;
        loss = null;

        if (!start.HasValue || !end.HasValue)
            return;

        var diff = end.Value - start.Value;

        if (diff > 0)
            gain = diff;
        else if (diff < 0)
            loss = -diff;
    }

    private static int? CalculateDifference(int? maxAlt, int? referenceAlt)
    {
        if (!maxAlt.HasValue || !referenceAlt.HasValue)
            return null;

        return maxAlt.Value - referenceAlt.Value;
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