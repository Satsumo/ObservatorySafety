
namespace ObservatorySafety.Core.Status
{
  public interface IStatusHandler
  {
    string Name { get; }

    StatusHandlerConfig Config { get; }

    void HandleMonitorStates(StatusPacket packet);
  }

}
