namespace FlightApp.Domain;

public class FlightStatsCalculator
{
    public FlightStats Calculate(List<FixPoint> fixes)
    {
        var stats = new FlightStats();

        if (fixes == null || fixes.Count == 0)
            return stats;

        // hier kommt später die Berechnung rein

        return stats;
    }
}