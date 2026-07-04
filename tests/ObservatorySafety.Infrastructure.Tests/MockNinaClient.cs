using ObservatorySafety.Core;

namespace ObservatorySafety.Infrastructure.Tests;

/// <summary>
/// A deterministic mock of INinaClient for integration testing.
/// Tracks all calls and never touches real hardware or HTTP.
/// </summary>
public class MockNinaClient : INinaClient
{
  public int AbortCameraExposureCount { get; private set; }
  public int AbortSequenceCount { get; private set; }
  public int StopSequenceCount { get; private set; }
  public int ParkCount { get; private set; }
  public int WarmCount { get; private set; }
  public int CloseCount { get; private set; }

  public List<string> CallLog { get; } = new();

  public Task AbortCameraExposureAsync()
  {
    AbortCameraExposureCount++;
    CallLog.Add("AbortCameraExposure");
    return Task.CompletedTask;
  }
  
  public Task AbortSequenceAsync()
  {
    AbortSequenceCount++;
    CallLog.Add("AbortSequence");
    return Task.CompletedTask;
  }

  public Task StopSequenceAsync()
  {
    StopSequenceCount++;
    CallLog.Add("StopSequence");
    return Task.CompletedTask;
  }

  public Task ParkMountAsync()
  {
    ParkCount++;
    CallLog.Add("ParkMount");
    return Task.CompletedTask;
  }

  public Task WarmCameraAsync()
  {
    WarmCount++;
    CallLog.Add("WarmCamera");
    return Task.CompletedTask;
  }

  public Task CloseDomeAsync()
  {
    CloseCount++;
    CallLog.Add("CloseDome");
    return Task.CompletedTask;
  }

  public async Task ExecuteShutdownAsync(ShutdownCommand cmd)
  {
    if (cmd.AbortCameraExposure) await AbortCameraExposureAsync();
    if (cmd.AbortSequence) await AbortSequenceAsync();
    if (cmd.StopSequence) await StopSequenceAsync();
    if (cmd.ParkMount) await ParkMountAsync();
    if (cmd.WarmCamera) await WarmCameraAsync();
    if (cmd.CloseDome) await CloseDomeAsync();
  }
}
