namespace FlightApp.Services;

public sealed class FlightImportStateService
{
    private FlightImportState _state = new();

    public FlightImportState State => _state;

    public event Action? OnChange;

    public void Start(int totalFiles)
    {
        _state = new FlightImportState
        {
            IsImporting = true,
            TotalFiles = totalFiles,
            ProcessedFiles = 0,
            CurrentFileName = null,
            CurrentMessage = null
        };

        NotifyStateChanged();
    }

    public void SetCurrentFile(string? fileName, string? message = null)
    {
        _state = _state with
        {
            CurrentFileName = fileName,
            CurrentMessage = message
        };

        NotifyStateChanged();
    }

    public void SetMessage(string? message)
    {
        _state = _state with
        {
            CurrentMessage = message
        };

        NotifyStateChanged();
    }

    public void Advance()
    {
        _state = _state with
        {
            ProcessedFiles = _state.ProcessedFiles + 1
        };

        NotifyStateChanged();
    }

    public void Finish(string? message = null)
    {
        _state = _state with
        {
            IsImporting = false,
            CurrentFileName = null,
            CurrentMessage = message
        };

        NotifyStateChanged();
    }

    public void Reset()
    {
        _state = new FlightImportState();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}