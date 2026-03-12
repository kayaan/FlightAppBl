// FlightApp/Analysis/TrackSegmentStatsCalculator.cs

namespace FlightApp.Analysis;

using FlightApp.Domain;

public class TrackSegmentStatsCalculator
{
    public FlightStats Calculate(TrackArrays track, SelectionRange range)
    {
        var stats = new FlightStats();

        if (track is null || !range.IsValid)
            return stats;

        int from = range.FromIndex;
        int to = range.ToIndex;

        if (track.TDeltaMs is null ||
            track.LatE7 is null ||
            track.LonE7 is null ||
            track.AltGpsCm is null ||
            track.AltBaroCm is null ||
            track.SpeedCms is null ||
            track.VarioCms is null)
        {
            return stats;
        }

        int length = track.LatE7.Length;

        if (track.TDeltaMs.Length != length ||
            track.LonE7.Length != length ||
            track.AltGpsCm.Length != length ||
            track.AltBaroCm.Length != length ||
            track.SpeedCms.Length != length ||
            track.VarioCms.Length != length)
        {
            return stats;
        }

        if (from < 0 || to >= length || from > to)
            return stats;

        stats.IsSegmentSelection = true;
        stats.SelectionStartIndex = from;
        stats.SelectionEndIndex = to;
        stats.FixCount = to - from + 1;

        long durationMs = 0;

        double gpsMinM = double.MaxValue;
        double gpsMaxM = double.MinValue;
        double baroMinM = double.MaxValue;
        double baroMaxM = double.MinValue;

        double totalDistanceMeters = 0;

        double totalSpeedKmh = 0;
        int speedCount = 0;
        double? maxSpeedKmh = null;

        double totalVarioMs = 0;
        int varioCount = 0;
        double? maxVarioMs = null;
        double? minVarioMs = null;

        double totalClimbMeters = 0;
        double totalSinkMeters = 0;

        for (int i = from; i <= to; i++)
        {
            double gpsM = CmToM(track.AltGpsCm[i]);
            double baroM = CmToM(track.AltBaroCm[i]);
            double speedKmh = CmsToKmh(track.SpeedCms[i]);
            double varioMs = CmsToMs(track.VarioCms[i]);

            if (gpsM < gpsMinM) gpsMinM = gpsM;
            if (gpsM > gpsMaxM) gpsMaxM = gpsM;

            if (baroM < baroMinM) baroMinM = baroM;
            if (baroM > baroMaxM) baroMaxM = baroM;

            totalSpeedKmh += speedKmh;
            speedCount++;

            if (maxSpeedKmh == null || speedKmh > maxSpeedKmh.Value)
                maxSpeedKmh = speedKmh;

            totalVarioMs += varioMs;
            varioCount++;

            if (maxVarioMs == null || varioMs > maxVarioMs.Value)
                maxVarioMs = varioMs;

            if (minVarioMs == null || varioMs < minVarioMs.Value)
                minVarioMs = varioMs;

            if (i > from)
            {
                durationMs += track.TDeltaMs[i];

                totalDistanceMeters += HaversineMeters(
                    track.LatE7[i - 1],
                    track.LonE7[i - 1],
                    track.LatE7[i],
                    track.LonE7[i]);

                double prevPreferredAltM = GetPreferredAltitudeM(track, i - 1);
                double currPreferredAltM = GetPreferredAltitudeM(track, i);

                double deltaAltM = currPreferredAltM - prevPreferredAltM;
                if (deltaAltM > 0)
                    totalClimbMeters += deltaAltM;
                else if (deltaAltM < 0)
                    totalSinkMeters += -deltaAltM;
            }
        }

        stats.Duration = TimeSpan.FromMilliseconds(durationMs);

        stats.AltGpsStart = MToNullableInt(CmToM(track.AltGpsCm[from]));
        stats.AltGpsEnd = MToNullableInt(CmToM(track.AltGpsCm[to]));
        stats.AltGpsMin = MToNullableInt(gpsMinM);
        stats.AltGpsMax = MToNullableInt(gpsMaxM);

        stats.AltBaroStart = MToNullableInt(CmToM(track.AltBaroCm[from]));
        stats.AltBaroEnd = MToNullableInt(CmToM(track.AltBaroCm[to]));
        stats.AltBaroMin = MToNullableInt(baroMinM);
        stats.AltBaroMax = MToNullableInt(baroMaxM);

        stats.TotalDistanceMeters = totalDistanceMeters;
        stats.AvgGroundSpeedKmh = speedCount > 0 ? totalSpeedKmh / speedCount : null;
        stats.MaxGroundSpeedKmh = maxSpeedKmh;

        stats.AvgVarioMs = varioCount > 0 ? totalVarioMs / varioCount : null;
        stats.MaxVarioMs = maxVarioMs;
        stats.MinVarioMs = minVarioMs;

        stats.TotalClimbMeters = totalClimbMeters;
        stats.TotalSinkMeters = totalSinkMeters;

        ApplyStartEndGainLossStats(
            stats,
            stats.AltGpsStart,
            stats.AltGpsEnd,
            stats.AltBaroStart,
            stats.AltBaroEnd);

        ApplyMaxAboveStartEndStats(
            stats,
            stats.AltGpsStart,
            stats.AltGpsEnd,
            stats.AltBaroStart,
            stats.AltBaroEnd,
            stats.AltGpsMax,
            stats.AltBaroMax);

        return stats;
    }

    private static double GetPreferredAltitudeM(TrackArrays track, int index)
    {
        int baroCm = track.AltBaroCm[index];
        if (baroCm != 0)
            return CmToM(baroCm);

        return CmToM(track.AltGpsCm[index]);
    }

    private static void ApplyStartEndGainLossStats(
        FlightStats stats,
        int? gpsStart,
        int? gpsEnd,
        int? baroStart,
        int? baroEnd)
    {
        ApplyGainLoss(gpsStart, gpsEnd, out var gpsGain, out var gpsLoss);
        ApplyGainLoss(baroStart, baroEnd, out var baroGain, out var baroLoss);

        stats.AltGpsGainMeters = gpsGain;
        stats.AltGpsLossMeters = gpsLoss;
        stats.AltBaroGainMeters = baroGain;
        stats.AltBaroLossMeters = baroLoss;
    }

    private static void ApplyMaxAboveStartEndStats(
        FlightStats stats,
        int? gpsStart,
        int? gpsEnd,
        int? baroStart,
        int? baroEnd,
        int? gpsMax,
        int? baroMax)
    {
        stats.MaxHeightAboveLaunchGps = CalculateDifference(gpsMax, gpsStart);
        stats.MaxHeightAboveLandingGps = CalculateDifference(gpsMax, gpsEnd);
        stats.MaxHeightAboveLaunchBaro = CalculateDifference(baroMax, baroStart);
        stats.MaxHeightAboveLandingBaro = CalculateDifference(baroMax, baroEnd);
    }

    private static void ApplyGainLoss(int? start, int? end, out int? gain, out int? loss)
    {
        gain = null;
        loss = null;

        if (!start.HasValue || !end.HasValue)
            return;

        int diff = end.Value - start.Value;

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

    private static double HaversineMeters(int lat1E7, int lon1E7, int lat2E7, int lon2E7)
    {
        double lat1 = lat1E7 / 10_000_000.0;
        double lon1 = lon1E7 / 10_000_000.0;
        double lat2 = lat2E7 / 10_000_000.0;
        double lon2 = lon2E7 / 10_000_000.0;

        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
            Math.Cos(lat1) * Math.Cos(lat2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return FlightConstants.EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double deg)
    {
        return deg * Math.PI / 180.0;
    }

    private static double CmToM(int cm) => cm / 100.0;

    private static double CmsToMs(int cms) => cms / 100.0;

    private static double CmsToKmh(int cms) => cms * 0.036;

    private static int? MToNullableInt(double meters) => (int)Math.Round(meters);
}