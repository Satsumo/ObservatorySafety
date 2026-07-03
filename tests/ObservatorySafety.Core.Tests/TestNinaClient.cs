using ObservatorySafety.Core;

namespace ObservatorySafety.Core.Tests;

public class TestNinaClient : INinaClient
{
  public int AbortCount { get; private set; }
  public int StopCount { get; private set; }
  public int ParkCount { get; private set; }
  public int WarmCount { get; private set; }
  public int CloseCount { get; private set; }

  public Task AbortSequenceAsync() { AbortCount++; return Task.CompletedTask; }
  public Task StopSequenceAsync() { StopCount++; return Task.CompletedTask; }
  public Task ParkMountAsync() { ParkCount++; return Task.CompletedTask; }
  public Task WarmCameraAsync() { WarmCount++; return Task.CompletedTask; }
  public Task CloseDomeAsync() { CloseCount++; return Task.CompletedTask; }
}
