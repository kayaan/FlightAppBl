using FlightApp.Domain;

namespace FlightApp.Services;

public enum FlightImportStatus
{
    Imported,
    Duplicate,
    InvalidIgc,
    Failed
}

public class FlightImportSaveResult
{
    public required Flight Flight { get; init; }
    public required FlightImportStatus Status { get; init; }

    public bool IsImported => Status == FlightImportStatus.Imported;
    public bool IsDuplicate => Status == FlightImportStatus.Duplicate;
}