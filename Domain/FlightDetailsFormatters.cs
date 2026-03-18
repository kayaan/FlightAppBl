using FlightApp.Analysis;
using FlightApp.Domain;

namespace FlightApp.Components;

public static class FlightDetailsFormatters
{
    public static string NullOrDash(string? value)
        => string.IsNullOrWhiteSpace(value) ? "—" : value;

    public static string FormatDateTimeShort(DateTime? value)
    {
        if (!value.HasValue)
            return "—";

        return value.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm");
    }

    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";

        return $"{duration.Minutes}m";
    }

    public static string FormatDistanceKm(double totalDistanceMeters)
        => $"{totalDistanceMeters / 1000.0:0.0} km";

    public static string FormatAltitudeValue(int? value)
    {
        if (!value.HasValue)
            return "—";

        return $"{value.Value} m";
    }

    public static string FormatAltitudeMinMaxPreferred(FlightStats stats)
    {
        var min = stats.AltBaroMin ?? stats.AltGpsMin;
        var max = stats.AltBaroMax ?? stats.AltGpsMax;

        if (!min.HasValue && !max.HasValue)
            return "—";

        var minText = min.HasValue ? $"{min.Value}" : "—";
        var maxText = max.HasValue ? $"{max.Value}" : "—";

        return $"{minText} / {maxText} m";
    }

    public static string FormatVario(FlightStats stats, Analysis.ClimbSegment? climb)
    {
        var min = stats.MinVarioMs;
        var max = stats.MaxVarioMs;

        if (!min.HasValue && !max.HasValue)
            return climb is null ? "—" : $"{climb.AvgClimbRateMs:0.0} m/s";

        var minText = min.HasValue ? $"{min.Value:0.0}" : "—";
        var maxText = max.HasValue ? $"{max.Value:0.0}" : "—";

        if (climb is null)
            return $"{minText} / {maxText} m/s";

        return $"{minText} / {maxText} / {climb.AvgClimbRateMs:0.0} m/s";
    }

    public static string FormatSpeedAvgMax(FlightStats stats)
    {
        var avgText = stats.AvgGroundSpeedKmh.HasValue ? $"{stats.AvgGroundSpeedKmh.Value:0.0}" : "—";
        var maxText = stats.MaxGroundSpeedKmh.HasValue ? $"{stats.MaxGroundSpeedKmh.Value:0.0}" : "—";

        return $"{avgText} / {maxText} km/h";
    }

    public static string FormatGain(ClimbSegment climb)
        => $"{climb.GainM:0} m";

    public static string FormatClockFromSeconds(double seconds)
    {
        var totalSeconds = Math.Max(0, (int)Math.Floor(seconds));
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var secs = totalSeconds % 60;

        return $"{hours}:{minutes:00}:{secs:00}";
    }

    public static string FormatDurationSeconds(double seconds)
        => FormatClockFromSeconds(seconds);

    public static string FormatMeters(double value)
        => $"{value:0} m";

    public static string FormatMs(double value)
        => $"{value:0.0} m/s";

    public static string FormatKmh(double value)
        => $"{value:0.0} km/h";
}