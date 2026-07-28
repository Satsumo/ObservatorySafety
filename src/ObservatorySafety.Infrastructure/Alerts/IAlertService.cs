using System.Threading;
using System.Threading.Tasks;

namespace ObservatorySafety.Infrastructure.Alerts
{
  public interface IAlertService
  {
    Task SendAlertAsync(string title, string message, CancellationToken cancellationToken);
  }
}
