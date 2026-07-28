
namespace ObservatorySafety.Core.Status
{
  public sealed class StatusChangedEventArgs : EventArgs
  {
    public MonitorState State { get; }

    public StatusChangedEventArgs(MonitorState state)
    {
      State = state;
    }
  }
}
