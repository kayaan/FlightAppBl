namespace FlightApp.Services;

public sealed class FlightImportStateService
{
    private FlightImportState _state = new();
    private readonly object _lock = new();

    public FlightImportState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    public event Action? OnChange;

    public void Start(int totalFiles)
    {
        lock (_lock)
        {
            _state = new FlightImportState
            {
                IsImporting = true,
                TotalFiles = totalFiles,
                ProcessedFiles = 0,
                ImportedCount = 0,
                DuplicateCount = 0,
                FailedCount = 0,
                CurrentFileName = null,
                CurrentMessage = "Starting import..."
            };
        }

        NotifyStateChanged();
    }

    public void SetCurrentFile(string? fileName, string? message = null)
    {
        lock (_lock)
        {
            _state = _state with
            {
                CurrentFileName = fileName,
                CurrentMessage = message
            };
        }

        NotifyStateChanged();
    }

    public void SetMessage(string? message)
    {
        lock (_lock)
        {
            _state = _state with
            {
                CurrentMessage = message
            };
        }

        NotifyStateChanged();
    }

    public void IncrementImported()
    {
        lock (_lock)
        {
            _state = _state with
            {
                ImportedCount = _state.ImportedCount + 1
            };
        }

        NotifyStateChanged();
    }

    public void IncrementDuplicate()
    {
        lock (_lock)
        {
            _state = _state with
            {
                DuplicateCount = _state.DuplicateCount + 1
            };
        }

        NotifyStateChanged();
    }

    public void IncrementFailed()
    {
        lock (_lock)
        {
            _state = _state with
            {
                FailedCount = _state.FailedCount + 1
            };
        }

        NotifyStateChanged();
    }

    public void Advance()
    {
        lock (_lock)
        {
            _state = _state with
            {
                ProcessedFiles = _state.ProcessedFiles + 1
            };
        }

        NotifyStateChanged();
    }

    public void Finish(string? message = null)
    {
        lock (_lock)
        {
            _state = _state with
            {
                IsImporting = false,
                CurrentFileName = null,
                CurrentMessage = message
            };
        }

        NotifyStateChanged();
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state = new FlightImportState();
        }

        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}