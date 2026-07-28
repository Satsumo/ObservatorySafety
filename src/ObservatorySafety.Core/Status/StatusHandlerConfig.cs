namespace ObservatorySafety.Core.Status
{
  public enum StatusNotificationType
  {
    OnChange,
    Poll
  }

  public sealed class StatusHandlerConfig
  {
    public StatusNotificationType NotificationType { get; init; }
    public TimeSpan? PollingInterval { get; init; } // meaningful only for Poll
  }

}
