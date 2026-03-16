namespace FlightApp.Analysis;

/// <summary>
/// Represents one KPI card in all-climbs mode.
/// Max and Min values are clickable and link to the corresponding climb.
/// </summary>
public sealed class ClimbSummaryKpi
{
    public required string Label { get; init; }

    public required string MaxText { get; init; }
    public required int MaxClimbIndex { get; init; }

    public required string MinText { get; init; }
    public required int MinClimbIndex { get; init; }

    public string? SubText { get; init; }
}