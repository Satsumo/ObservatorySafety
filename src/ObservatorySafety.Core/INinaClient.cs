namespace ObservatorySafety.Core;

public interface INinaClient
{
  Task AbortSequenceAsync();
  Task StopSequenceAsync();
  Task ParkMountAsync();
  Task WarmCameraAsync();
  Task CloseDomeAsync();
}
