namespace ObservatorySafety.Core.Status
{

  public abstract class StatusHandlerBase : IStatusHandler, IDisposable
  {
    public abstract string Name { get; }
    public abstract StatusHandlerConfig Config { get; }

    public abstract void HandleMonitorStates(StatusPacket packet);

    public abstract void Dispose();
  }
}
