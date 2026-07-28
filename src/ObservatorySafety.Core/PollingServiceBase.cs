namespace ObservatorySafety.Core
{
  public abstract class PollingServiceBase
  {
    private readonly int _pollingPeriodSeconds;
    private Timer? _timer;

    protected PollingServiceBase(int pollingPeriodSeconds = 10)
    {
      _pollingPeriodSeconds = pollingPeriodSeconds;
    }

    public void Start()
    {
      // Poll every _pollingPeriodSeconds seconds
      _timer = new Timer(_ => Poll(), null, TimeSpan.Zero, TimeSpan.FromSeconds(_pollingPeriodSeconds));
    }

    public void Stop()
    {
      _timer?.Dispose();
      _timer = null;
    }

    protected abstract void Poll();

  }
}
