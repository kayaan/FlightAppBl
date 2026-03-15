using System;
using System.Collections.Generic;
using FlightApp.Domain;

namespace FlightApp.Analysis;

/// <summary>
/// Detects climb phases in a flight track based on a peak-following algorithm.
///
/// A climb starts at a given point and continues as long as the altitude
/// does not fall too far below the highest altitude reached so far.
/// If the drop from the current peak exceeds the allowed threshold,
/// the climb segment ends at the peak point.
///
/// A segment is only accepted if its altitude gain reaches the configured minimum.
/// </summary>
public static class ClimbDetector
{
    /// <summary>
    /// Detects climb phases from the given track arrays.
    /// </summary>
    public static List<ClimbSegment> DetectClimbs(
        TrackArrays track,
        ClimbDetectionOptions? options = null)
    {
        options ??= new ClimbDetectionOptions();

        int pointCount = GetPointCount(track);
        int[] altitudeCm = SelectAltitudeArray(track, options, pointCount);
        int[] cumulativeTimeMs = BuildCumulativeTimeMs(track, pointCount);
        int[] speedCms = GetSpeedArray(track, pointCount);

        var result = new List<ClimbSegment>();

        if (pointCount < 2)
            return result;

        int minGainCm = (int)Math.Round(options.MinGainM * 100.0);
        int minAbsoluteDropCm = (int)Math.Round(options.MinAbsoluteDropM * 100.0);

        int startIndex = 0;

        while (startIndex < pointCount - 1)
        {
            int startAltitudeCm = altitudeCm[startIndex];
            int peakIndex = startIndex;
            int peakAltitudeCm = startAltitudeCm;

            int i = startIndex + 1;

            while (i < pointCount)
            {
                int currentAltitudeCm = altitudeCm[i];

                if (currentAltitudeCm > peakAltitudeCm)
                {
                    peakAltitudeCm = currentAltitudeCm;
                    peakIndex = i;
                    i++;
                    continue;
                }

                int gainCm = peakAltitudeCm - startAltitudeCm;
                int dropCm = peakAltitudeCm - currentAltitudeCm;

                int allowedDropCm = Math.Max(
                    minAbsoluteDropCm,
                    (int)Math.Round(gainCm * options.AllowedDropPercent));

                if (dropCm > allowedDropCm)
                    break;

                i++;
            }

            int segmentGainCm = peakAltitudeCm - startAltitudeCm;

            if (peakIndex > startIndex && segmentGainCm >= minGainCm)
            {
                double startTimeSec = cumulativeTimeMs[startIndex] / 1000.0;
                double endTimeSec = cumulativeTimeMs[peakIndex] / 1000.0;
                double durationSec = endTimeSec - startTimeSec;

                if (durationSec > 0)
                {
                    double startAltitudeM = startAltitudeCm / 100.0;
                    double endAltitudeM = peakAltitudeCm / 100.0;
                    double gainM = segmentGainCm / 100.0;
                    double avgClimbRateMs = gainM / durationSec;

                    var (avgSpeedKmh, maxSpeedKmh) = CalculateSpeedMetrics(
                        speedCms,
                        startIndex,
                        peakIndex);

                    result.Add(new ClimbSegment
                    {
                        BeginIndex = startIndex,
                        EndIndex = peakIndex,
                        StartTimeSec = startTimeSec,
                        EndTimeSec = endTimeSec,
                        DurationSec = durationSec,
                        StartAltitudeM = startAltitudeM,
                        EndAltitudeM = endAltitudeM,
                        GainM = gainM,
                        AvgClimbRateMs = avgClimbRateMs,
                        AvgSpeedKmh = avgSpeedKmh,
                        MaxSpeedKmh = maxSpeedKmh
                    });
                }
            }

            // Continue after the detected peak to avoid overlapping climb segments.
            startIndex = peakIndex + 1;
        }

        return result;
    }

    /// <summary>
    /// Selects the altitude source used for climb detection.
    /// Barometric altitude is preferred if configured and available.
    /// GPS altitude is used as fallback.
    /// </summary>
    private static int[] SelectAltitudeArray(
        TrackArrays track,
        ClimbDetectionOptions options,
        int pointCount)
    {
        bool hasBaro = track.AltBaroCm is { Length: > 0 } && track.AltBaroCm.Length == pointCount;
        bool hasGps = track.AltGpsCm is { Length: > 0 } && track.AltGpsCm.Length == pointCount;

        if (options.PreferBarometricAltitude && hasBaro)
            return track.AltBaroCm;

        if (hasGps)
            return track.AltGpsCm;

        if (hasBaro)
            return track.AltBaroCm;

        throw new InvalidOperationException("No valid altitude array is available for climb detection.");
    }

    /// <summary>
    /// Returns the speed array used for speed metrics.
    /// If no valid speed array is available, a zero-filled array is returned.
    /// </summary>
    private static int[] GetSpeedArray(TrackArrays track, int pointCount)
    {
        if (track.SpeedCms is { Length: > 0 } && track.SpeedCms.Length == pointCount)
            return track.SpeedCms;

        return new int[pointCount];
    }

    /// <summary>
    /// Builds cumulative time values in milliseconds from TDeltaMs.
    ///
    /// Assumption:
    /// TDeltaMs[i] is the time delta from point i - 1 to point i.
    /// Therefore the cumulative time for index 0 is 0 ms.
    /// </summary>
    private static int[] BuildCumulativeTimeMs(TrackArrays track, int pointCount)
    {
        if (track.TDeltaMs.Length != pointCount)
            throw new InvalidOperationException("TDeltaMs length does not match track point count.");

        var cumulativeTimeMs = new int[pointCount];

        for (int i = 1; i < pointCount; i++)
        {
            cumulativeTimeMs[i] = cumulativeTimeMs[i - 1] + track.TDeltaMs[i];
        }

        return cumulativeTimeMs;
    }

    /// <summary>
    /// Calculates average and maximum horizontal speed for the given segment.
    /// Input speeds are expected in centimeters per second.
    /// Output values are returned in kilometers per hour.
    /// </summary>
    private static (double AvgSpeedKmh, double MaxSpeedKmh) CalculateSpeedMetrics(
        int[] speedCms,
        int beginIndex,
        int endIndex)
    {
        if (beginIndex < 0 || endIndex < beginIndex || endIndex >= speedCms.Length)
            return (0.0, 0.0);

        long sumSpeedCms = 0;
        int maxSpeedCms = 0;
        int count = endIndex - beginIndex + 1;

        for (int i = beginIndex; i <= endIndex; i++)
        {
            int speed = speedCms[i];
            if (speed < 0)
                speed = 0;

            sumSpeedCms += speed;

            if (speed > maxSpeedCms)
                maxSpeedCms = speed;
        }

        double avgSpeedCms = count > 0 ? (double)sumSpeedCms / count : 0.0;

        return (CmsToKmh(avgSpeedCms), CmsToKmh(maxSpeedCms));
    }

    /// <summary>
    /// Converts centimeters per second to kilometers per hour.
    /// </summary>
    private static double CmsToKmh(double speedCms)
        => speedCms * 0.036;

    /// <summary>
    /// Determines the number of track points from the coordinate arrays.
    /// </summary>
    private static int GetPointCount(TrackArrays track)
    {
        if (track.LatE7.Length == 0 || track.LonE7.Length == 0)
            throw new InvalidOperationException("Track coordinate arrays must not be empty.");

        if (track.LatE7.Length != track.LonE7.Length)
            throw new InvalidOperationException("LatE7 and LonE7 length mismatch.");

        return track.LatE7.Length;
    }
}
