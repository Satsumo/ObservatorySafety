namespace ObservatorySafety.Core;

public class PowerLossDebouncer
{
  private readonly TimeSpan _threshold;
  private CancellationTokenSource? _cts;

  public event EventHandler? PowerLossConfirmed;

  public PowerLossDebouncer(TimeSpan threshold)
  {
    _threshold = threshold;
  }

  public void OnStatusChanged(PowerStatus status)
  {
    if (!status.IsOnGrid && status.IsCritical)
    {
      if (_cts == null)
      {
        _cts = new CancellationTokenSource();
        _ = WaitAndConfirmAsync(_cts.Token);
      }
    }
    else
    {
      _cts?.Cancel();
      _cts = null;
    }
  }

  private async Task WaitAndConfirmAsync(CancellationToken token)
  {
    try
    {
      await Task.Delay(_threshold, token);
      PowerLossConfirmed?.Invoke(this, EventArgs.Empty);
    }
    catch (TaskCanceledException)
    {
      // Power restored before threshold
    }
    finally
    {
      _cts = null;
    }
  }
}
