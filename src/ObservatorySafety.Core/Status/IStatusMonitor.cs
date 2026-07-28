using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Core.Abstractions
{
  public interface IStatusMonitor
  {
    MonitorType MonitorType { get; }

    IDictionary<StatusType, bool> Statuses { get; }

    event EventHandler<StatusChangedEventArgs>? StatusChanged;

    void Start();

    void Stop();
  }
   
}
