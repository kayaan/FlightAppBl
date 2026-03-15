namespace FlightApp.Analysis;

/// <summary>
/// Represents a detected climb phase within a flight track.
/// A climb segment starts at BeginIndex and ends at EndIndex,
/// where EndIndex corresponds to the altitude peak before the
/// allowed altitude drop threshold was exceeded.
/// </summary>
public sealed class ClimbSegment
{
    /// <summary>
    /// Index of the first track point of the climb phase.
    /// </summary>
    public int BeginIndex { get; init; }

    /// <summary>
    /// Index of the last track point of the climb phase.
    /// This corresponds to the altitude peak of the segment.
    /// </summary>
    public int EndIndex { get; init; }

    /// <summary>
    /// Time in seconds at the start of the climb phase.
    /// </summary>
    public double StartTimeSec { get; init; }

    /// <summary>
    /// Time in seconds at the end of the climb phase.
    /// </summary>
    public double EndTimeSec { get; init; }

    /// <summary>
    /// Duration of the climb phase in seconds.
    /// </summary>
    public double DurationSec { get; init; }

    /// <summary>
    /// Altitude at the beginning of the climb phase, in meters.
    /// </summary>
    public double StartAltitudeM { get; init; }

    /// <summary>
    /// Altitude at the end of the climb phase, in meters.
    /// </summary>
    public double EndAltitudeM { get; init; }

    /// <summary>
    /// Net altitude gain during the climb phase, in meters.
    /// </summary>
    public double GainM { get; init; }

    /// <summary>
    /// Average climb rate over the segment, in meters per second.
    /// </summary>
    public double AvgClimbRateMs { get; init; }

    /// <summary>
    /// Average horizontal speed over the segment, in kilometers per hour.
    /// </summary>
    public double AvgSpeedKmh { get; init; }

    /// <summary>
    /// Maximum horizontal speed within the segment, in kilometers per hour.
    /// </summary>
    public double MaxSpeedKmh { get; init; }
}
