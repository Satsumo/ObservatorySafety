using System.Threading;
using System.Threading.Tasks;

namespace ObservatorySafety.Watchdog.Alerts
{
  public interface IAlertService
  {
    Task SendAlertAsync(string title, string message, CancellationToken cancellationToken);
  }
}
