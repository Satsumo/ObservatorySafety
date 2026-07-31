using ObservatorySafety.Core.Options;
using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Infrastructure.Options
{
  public class DragonflyOptions: AscomMonitorOptions
  {
    public StatusType[] SensorStatusTypes { get; set; } = [];
  }
}
