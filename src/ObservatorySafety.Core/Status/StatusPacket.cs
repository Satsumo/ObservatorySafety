
namespace ObservatorySafety.Core.Status
{
  public sealed class StatusPacket
  {
    public DateTime TimestampUtc { get; init; }
    public IReadOnlyDictionary<MonitorType, MonitorState> MonitorStates { get; init; } =
        new Dictionary<MonitorType, MonitorState>();
  }
}
