namespace FlightApp.Services;

public sealed record FlightImportState
{
    public bool IsImporting { get; init; }
    public int TotalFiles { get; init; }
    public int ProcessedFiles { get; init; }

    public int ImportedCount { get; init; }
    public int DuplicateCount { get; init; }
    public int FailedCount { get; init; }

    public string? CurrentFileName { get; init; }
    public string? CurrentMessage { get; init; }
}