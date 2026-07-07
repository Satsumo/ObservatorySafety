using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Model;

namespace ObservatorySafety.Infrastructure;

public class NinaScalarClient : INinaClient
{
  private readonly ILogger<NinaScalarClient> _logger = LogProvider.Factory.CreateLogger<NinaScalarClient>();
  private readonly HttpService _httpService;

  public NinaScalarClient(HttpService httpService)
  {
    _httpService = httpService;
  }

  public async Task<EquipmentInfoEnvelope> GetEquipmentInfoAsync()
  {
    try
    {     
      var resp = await _httpService.Call(HttpMethod.Get, INinaClient.API_EQUIPMENT_INFO);           
      var json = await resp.Content.ReadAsStringAsync();

      return JsonSerializer.Deserialize<EquipmentInfoEnvelope>(json)!;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting equipment info!");
      throw;
    }
  }

  public async Task<bool> IsNinaRunningAsync()
  {
    try
    {
      await _httpService.Call(HttpMethod.Get, INinaClient.API_VERSION);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to get NINA version - assuming NINA is not running!");
      return false;
    }
  }

  public Task StopSequenceAsync() => _httpService.Call(HttpMethod.Get, INinaClient.API_STOP_SEQUENCE);
  public Task ParkMountAsync() => _httpService.Call(HttpMethod.Get, INinaClient.API_PARK_MOUNT);
  public Task WarmCameraAsync() => _httpService.Call(HttpMethod.Get, INinaClient.API_WARM_CAMERA);
  public Task CloseDomeAsync() => _httpService.Call(HttpMethod.Get, INinaClient.API_CLOSE_DOME);
  public async Task ExecuteShutdownAsync(ShutdownCommand cmd)
  {
    var isNinaRunning = await IsNinaRunningAsync();
    if (!isNinaRunning) {
      _logger.LogWarning("NINA is not running. Shutdown not neccesary!");
      return;
    }

    _logger.LogInformation("Starting shutdown...");

    if (cmd.StopSequence)
    {
      _logger.LogInformation("Stopping sequence...");
      await StopSequenceAsync();
      await WaitUntil(async () => !await IsSequenceRunningAsync(),
          "Sequence did not stop");
      _logger.LogInformation("Sequence stopped.");
    }

    if (cmd.ParkMount)
    {
      _logger.LogInformation("Parking mount...");
      await ParkMountAsync();
      await WaitUntil(async () => await IsMountParkedAsync(),
          "Mount did not park or it is still slewing/tracking", 5000);
      _logger.LogInformation("Mount parked.");
    }

    if (cmd.WarmCamera)
    {
      _logger.LogInformation("Warming camera...");
      await WarmCameraAsync();
      await WaitUntil(async () => await IsCameraWarmingAsync(),
          "Camera did not warm");
      _logger.LogInformation("Camera warmed.");
    }

    if (cmd.CloseDome)
    {
      _logger.LogInformation("Closing dome...");
      await CloseDomeAsync();
      await WaitUntil(async () => await IsDomeClosedAsync(),
          "Dome did not close", 10000);
      _logger.LogInformation("Dome closed.");
    }

    _logger.LogInformation("Shutdown completed.");
  }

  private async Task<bool> IsMountParkedAsync()
  {
    var m = (await GetEquipmentInfoAsync()).Response?.Mount;
    return m != null && (!m.Connected || (m.AtPark && !m.Slewing && !m.TrackingEnabled));
  }

  private async Task<bool> IsDomeClosedAsync()
  {
    var d = (await GetEquipmentInfoAsync()).Response?.Dome;
    return d != null && (!d.Connected || d.ShutterStatus == "ShutterClosed");
  }

  private async Task<bool> IsCameraWarmingAsync()
  {
    var c = (await GetEquipmentInfoAsync()).Response?.Camera;
    return c != null && (!c.Connected || !c.CoolerOn);
  }

  private async Task<bool> IsSequenceRunningAsync()
  {
    var s = (await GetEquipmentInfoAsync()).Response?.Sequence;
    return s?.IsRunning ?? false;
  }

  private async Task WaitUntil(Func<Task<bool>> condition, string failureMessage, int pollingDelay = 1000, int timeoutSeconds = 60)
  {
    var start = DateTime.UtcNow;

    while ((DateTime.UtcNow - start).TotalSeconds < timeoutSeconds)
    {
      if (await condition())
        return;

      await Task.Delay(pollingDelay);
    }

    throw new Exception(failureMessage);
  }

}
