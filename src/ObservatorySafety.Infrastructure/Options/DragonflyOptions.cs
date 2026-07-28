using ObservatorySafety.Core.Options;
using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Infrastructure.Options
{
  public class DragonflyOptions: AscomOptionsBase
  {
    public StatusType[] StatusTypes { get; set; } = [];
  }
}
