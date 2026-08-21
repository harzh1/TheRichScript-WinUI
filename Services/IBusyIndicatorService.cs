using System;

namespace TheRichScript_WinUI.Services;

public interface IBusyIndicatorService
{
    bool IsBusy { get; }
    string? BusyMessage { get; }

    event Action<bool, string?>? BusyStateChanged;

    void Show(string message = "Processing request...");
    void Hide();
    IDisposable BeginBusy(string message = "Processing request...");
}

public class BusyIndicatorService : IBusyIndicatorService
{
    private int _busyCount = 0;
    private readonly object _lock = new();

    public bool IsBusy => _busyCount > 0;
    public string? BusyMessage { get; private set; }

    public event Action<bool, string?>? BusyStateChanged;

    public void Show(string message = "Processing request...")
    {
        lock (_lock)
        {
            _busyCount++;
            BusyMessage = message;
            Notify();
        }
    }

    public void Hide()
    {
        lock (_lock)
        {
            if (_busyCount > 0)
            {
                _busyCount--;
            }

            if (_busyCount == 0)
            {
                BusyMessage = null;
            }

            Notify();
        }
    }

    public IDisposable BeginBusy(string message = "Processing request...")
    {
        Show(message);
        return new BusyScope(this);
    }

    private void Notify()
    {
        BusyStateChanged?.Invoke(IsBusy, BusyMessage);
    }

    private sealed class BusyScope : IDisposable
    {
        private readonly BusyIndicatorService _service;
        private bool _disposed;

        public BusyScope(BusyIndicatorService service)
        {
            _service = service;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _service.Hide();
            }
        }
    }
}
