namespace ObservatorySafety.Core;

public class ShutdownOrchestrator
{
  public ShutdownCommand? GetCommandFor(PowerStatus status)
  {
    if (!status.IsOnGrid && status.IsCritical)
    {
      return new ShutdownCommand(
          StopSequence: true,
          ParkMount: true,
          WarmCamera: true,
          CloseDome: true
      );
    }

    return null;
  }

  public async Task ExecuteAsync(ShutdownCommand cmd, INinaClient nina)
  {
    if (cmd.StopSequence) await nina.StopSequenceAsync();
    if (cmd.ParkMount) await nina.ParkMountAsync();
    if (cmd.WarmCamera) await nina.WarmCameraAsync();
    if (cmd.CloseDome) await nina.CloseDomeAsync();
  }
}
