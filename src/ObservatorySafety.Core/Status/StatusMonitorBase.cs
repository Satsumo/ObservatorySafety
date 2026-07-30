using Microsoft.Extensions.Logging;

using ObservatorySafety.Core.Abstractions;

using static System.Runtime.InteropServices.JavaScript.JSType;

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
          // Each monitor is configured to only provide certain statuses (via config).
          // This allows us to control which statuses come from which providers via config even
          // though a status can come from several monitors (for example, mount parked can come from
          // NINA, Mount Sensor and even a Dragonfly).  This allows us to use the config to drive
          // which monitor we trust to be the best source.
          // So, we remove any status values that are not in this monitor's configuration.
          var filtered = value
            .Where(kvp => this.ProvidedStatuses.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

          // If the values haven't changed then do nothing
          if (_statuses.OrderBy(k => k.Key).SequenceEqual(filtered.OrderBy(k => k.Key))) 
            return;

          _statuses = value;

          var statusesAsString = string.Join("\r\n", _statuses.Select(kvp => $"{kvp.Key}={kvp.Value}"));
          this.Logger.LogInformation("Monitor {name}: Status changed to:\r\n{statuses}", this.MonitorType, statusesAsString);

          OnStatusChanged();
        }
      }
    }

    public abstract MonitorType MonitorType { get; }

    public abstract StatusType[] ProvidedStatuses { get; }


    public abstract ILogger Logger { get; }

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
