
namespace ObservatorySafety.Core.Status
{
  public interface IPollingStatusHandler : IStatusHandler
  {
    TimeSpan PollingInterval { get; }
  }
}
