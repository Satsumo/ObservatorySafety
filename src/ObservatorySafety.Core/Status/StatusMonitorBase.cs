using ObservatorySafety.Core.Abstractions;

namespace ObservatorySafety.Core.Status
{
  public abstract class StatusMonitorBase : PollingServiceBase, IStatusMonitor
  {
    private readonly object _sync = new();
    private IDictionary<StatusType, bool> _statuses = new Dictionary<StatusType, bool>();

    public event EventHandler<StatusChangedEventArgs>? StatusChanged;
    
    public StatusMonitorBase(int pollingPeriodSeconds = 10) : base(pollingPeriodSeconds)
    { 
    }

    public IDictionary<StatusType, bool> Statuses
    {
      get { 
        lock (_sync) return _statuses;
      }
      protected set
      {
        lock (_sync)
        {
          if (_statuses.OrderBy(k => k.Key).SequenceEqual(value.OrderBy(k => k.Key))) 
            return;

          _statuses = value;
          OnStatusChanged();
        }
      }
    }

    public abstract MonitorType MonitorType { get; }    

    protected void RaiseStatusChanged()
    {
      var state = new MonitorState
      {
        MonitorType = MonitorType,
        Statuses = Statuses,
        TimestampUtc = DateTime.UtcNow
      };

      StatusChanged?.Invoke(this, new StatusChangedEventArgs(state));
    }

    protected virtual void OnStatusChanged() => RaiseStatusChanged();

  }

}
