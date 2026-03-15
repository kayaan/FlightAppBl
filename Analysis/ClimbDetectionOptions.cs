using System;

namespace FlightApp.Analysis;

/// <summary>
/// Configuration options for climb phase detection.
/// </summary>
public sealed class ClimbDetectionOptions
{
    /// <summary>
    /// Minimum altitude gain required for a valid climb segment, in meters.
    /// Default: 25 m.
    /// </summary>
    public double MinGainM { get; init; } = 25.0;

    /// <summary>
    /// Maximum allowed relative drop from the current peak.
    /// Example: 0.05 means 5% of the current gain from segment start to peak.
    /// </summary>
    public double AllowedDropPercent { get; init; } = 0.05;

    /// <summary>
    /// Minimum absolute drop tolerance in meters.
    /// This prevents the detector from becoming too sensitive
    /// when the current gain is still very small.
    /// Default: 2 m.
    /// </summary>
    public double MinAbsoluteDropM { get; init; } = 2.0;

    /// <summary>
    /// When true, barometric altitude is preferred over GPS altitude.
    /// If barometric altitude is unavailable, GPS altitude is used as fallback.
    /// </summary>
    public bool PreferBarometricAltitude { get; init; } = true;
}
