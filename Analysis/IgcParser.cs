using FlightApp.Domain;

namespace FlightApp.Analysis;

public class IgcParser
{
    public IgcHeader ParseHeader(string content)
    {
        var lines = content.Split('\n');

        DateOnly? date = null;
        string? pilot = null;
        string? glider = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("HFDTEDATE"))
            {
                var value = trimmed.Split(':')[1];

                var day = int.Parse(value.Substring(0, 2));
                var month = int.Parse(value.Substring(2, 2));
                var year = 2000 + int.Parse(value.Substring(4, 2));

                date = new DateOnly(year, month, day);
            }
            else if (trimmed.StartsWith("HFDTE"))
            {
                // Klassisches IGC-Format: HFDTE280524
                var day = int.Parse(trimmed.Substring(5, 2));
                var month = int.Parse(trimmed.Substring(7, 2));
                var year = 2000 + int.Parse(trimmed.Substring(9, 2));

                date = new DateOnly(year, month, day);
            }
            else if (trimmed.StartsWith("HFPLTPILOT"))
            {
                pilot = GetHeaderValue(trimmed);
            }
            else if (trimmed.StartsWith("HFGTYGLIDERTYPE"))
            {
                glider = GetHeaderValue(trimmed);
            }
        }

        return new IgcHeader(date, pilot, glider);
    }

    public List<FixPoint> ParseFixes(string content)
    {
        var fixes = new List<FixPoint>();
        var lines = content.Split('\n');

        var header = ParseHeader(content);

        if (header.Date is null)
            return fixes;

        var currentDate = header.Date.Value;
        TimeOnly? previousTime = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (!trimmed.StartsWith("B"))
                continue;

            try
            {
                var time = ParseTime(trimmed);

                // Mitternacht erkennen
                if (previousTime.HasValue && time < previousTime.Value)
                {
                    currentDate = currentDate.AddDays(1);
                }

                var timestamp = new DateTime(
                    currentDate.Year,
                    currentDate.Month,
                    currentDate.Day,
                    time.Hour,
                    time.Minute,
                    time.Second,
                    DateTimeKind.Utc);

                var lat = ParseLatitude(trimmed);
                var lon = ParseLongitude(trimmed);
                var altBaro = ParseBaroAltitude(trimmed);
                var altGps = ParseGpsAltitude(trimmed);

                var fix = new FixPoint
                {
                    TimeUtc = timestamp,
                    Latitude = lat,
                    Longitude = lon,
                    AltitudeBaro = altBaro,
                    AltitudeGps = altGps
                };

                fixes.Add(fix);

                previousTime = time;
            }
            catch
            {
                // erstmal ignorieren
            }
        }

        return fixes;
    }


    private static string? GetHeaderValue(string line)
    {
        var idx = line.IndexOf(':');
        if (idx < 0 || idx == line.Length - 1)
            return null;

        return line[(idx + 1)..].Trim();
    }

    private TimeOnly ParseTime(string line)
    {
        var hh = int.Parse(line.Substring(1, 2));
        var mm = int.Parse(line.Substring(3, 2));
        var ss = int.Parse(line.Substring(5, 2));

        return new TimeOnly(hh, mm, ss);
    }

    private double ParseLatitude(string line)
    {
        var deg = int.Parse(line.Substring(7, 2));
        var min = int.Parse(line.Substring(9, 5)) / 1000.0;

        var lat = deg + min / 60.0;

        if (line[14] == 'S')
            lat = -lat;

        return lat;
    }

    private double ParseLongitude(string line)
    {
        var deg = int.Parse(line.Substring(15, 3));
        var min = int.Parse(line.Substring(18, 5)) / 1000.0;

        var lon = deg + min / 60.0;

        if (line[23] == 'W')
            lon = -lon;

        return lon;
    }

    private int ParseGpsAltitude(string line)
    {
        return int.Parse(line.Substring(30, 5));
    }

    private int ParseBaroAltitude(string line)
    {
        return int.Parse(line.Substring(25, 5));
    }
}