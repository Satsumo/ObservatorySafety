
namespace ObservatorySafety.Core.Status
{
  public class MonitorState
  {
    public MonitorType MonitorType { get; init; }

    public IDictionary<StatusType, bool> Statuses { get; init; } = new Dictionary<StatusType, bool>();

    public DateTime TimestampUtc { get; init; }
  }

}
