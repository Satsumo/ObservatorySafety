using ObservatorySafety.Core;

namespace ObservatorySafety.Infrastructure;

public class StatusFileWatcher : IDisposable
{
  private readonly FileSystemWatcher _watcher;
  private readonly string _flagFile;

  public event EventHandler<PowerStatus>? StatusChanged;

  public StatusFileWatcher(SafetyOptions options)
  {
    _flagFile = options.FlagFilePath;

    _watcher = new FileSystemWatcher(
        Path.GetDirectoryName(_flagFile)!,
        Path.GetFileName(_flagFile)
    )
    {
      NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
    };

    _watcher.Created += (_, __) => Emit();
    _watcher.Deleted += (_, __) => Emit();
    _watcher.Changed += (_, __) => Emit();
    _watcher.EnableRaisingEvents = true;

    Emit();
  }

  private void Emit()
  {
    bool exists = File.Exists(_flagFile);

    var status = new PowerStatus(
        IsOnGrid: !exists,
        IsCritical: exists
    );

    StatusChanged?.Invoke(this, status);
  }

  public void Dispose()
  {
    _watcher.Dispose();
  }
}
