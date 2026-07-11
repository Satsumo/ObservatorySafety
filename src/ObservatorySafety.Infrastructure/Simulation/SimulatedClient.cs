using Microsoft.Extensions.Logging;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Model;

namespace ObservatorySafety.Infrastructure.Simulation;

/// <summary>
/// A deterministic mock of INinaClient for integration testing.
/// Tracks all calls and never touches real hardware or HTTP.
/// </summary>
public class SimulatedClient : IAstronomyApplicationClient
{
  private readonly ILogger<SimulatedClient> _logger;
  public SimulatedClient(ILogger<SimulatedClient> logger)
  {
    _logger = logger;
  }

  public int IsNinaRunningCount { get; private set; }
  public int GetMountInfoCount { get; private set; }
  public int StopSequenceCount { get; private set; }
  public int ParkCount { get; private set; }
  public int WarmCount { get; private set; }
  public int CloseCount { get; private set; }

  public List<string> CallLog { get; } = new();

  public Task<bool> IsNinaRunningAsync()
  {
    IsNinaRunningCount++;
    AddCallLog("IsNinaRunning");
    return Task.FromResult(true);
  }

  public Task<EquipmentInfoEnvelope> GetEquipmentInfoAsync()
  {
    GetMountInfoCount++;
    AddCallLog("GetMountInfo");
    var envelope = new EquipmentInfoEnvelope
    {
      Response = new EquipmentInfo
      {
        Camera = new EquipmentCameraInfo { CoolerOn = true },
        Dome = new EquipmentDomeInfo { ShutterStatus = "ShutterOpen", Slewing = false, AtPark = false, Connected = true },
        Mount = new EquipmentMountInfo { AtPark = false, Slewing = false, TrackingEnabled = true, Connected = true },
        Sequence = new EquipmentSequenceInfo { IsRunning = true },
        SafetyMonitor = new EquipmentSafetyMonitorInfo { IsSafe = true }
      },
      Error = null,
      StatusCode = 200,
      Success = true,
      Type = "EquipmentInfo"
    };
    return Task.FromResult(envelope);
  }

  public Task StopSequenceAsync()
  {
    StopSequenceCount++;
    AddCallLog("StopSequence");
    return Task.CompletedTask;
  }

  public Task ParkMountAsync()
  {
    ParkCount++;
    AddCallLog("ParkMount");
    return Task.CompletedTask;
  }

  public Task WarmCameraAsync()
  {
    WarmCount++;
    AddCallLog("WarmCamera");
    return Task.CompletedTask;
  }

  public Task CloseDomeAsync()
  {
    CloseCount++;
    AddCallLog("CloseDome");
    return Task.CompletedTask;
  }

  public async Task ExecuteShutdownAsync(ShutdownCommand cmd)
  {
    _logger.LogInformation("Starting shutdown...");

    if (cmd.StopSequence) await StopSequenceAsync();
    if (cmd.ParkMount) await ParkMountAsync();
    if (cmd.WarmCamera) await WarmCameraAsync();
    if (cmd.CloseDome) await CloseDomeAsync();

    _logger.LogInformation("Shutdown complete.");

  }

  private List<string> AddCallLog(string callLogEntry)
  {
    CallLog.Add(callLogEntry);
    _logger.LogInformation("DryRunClient: {CallLogEntry}", callLogEntry);
    return CallLog;
  }
}
