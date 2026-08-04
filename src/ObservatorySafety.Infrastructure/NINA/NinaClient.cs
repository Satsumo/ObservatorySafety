using System.Text.Json;

using Microsoft.Extensions.Logging;

using ObservatorySafety.Core;
using ObservatorySafety.NINA.Model;

namespace ObservatorySafety.NINA;

public class NinaClient : INinaClient
{
  private readonly ILogger<NinaClient> _logger;
  private readonly IHttpService _httpService;
  private readonly EquipmentOptions _equipmentOptions;

  public NinaClient(ILogger<NinaClient> logger, IHttpService httpService, EquipmentOptions equipmentOptions)
  {
    _logger = logger;
    _httpService = httpService;
    _equipmentOptions = equipmentOptions;
  }

  public async Task<EquipmentInfoEnvelope?> GetEquipmentInfoAsync()
  {
    try
    {     
      var resp = await _httpService.Call(HttpMethod.Get, INinaClient.API_EQUIPMENT_INFO);           
      var json = await resp.Content.ReadAsStringAsync();

      return JsonSerializer.Deserialize<EquipmentInfoEnvelope>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
    catch (HttpRequestException ex) {
      _logger.LogError("NINA is not responding/running - unable to get equipment info: {Message}", ex.Message);
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected exception getting equipment info: {Message}", ex.Message);
      return null;
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
      _logger.LogWarning(ex, "Failed to get NINA version - assuming NINA is not running: {Message}", ex.Message);
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
      _logger.LogWarning("NINA ERROR: It is not responding.  Shutdown not possible/necessary.");
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
          "MOUNT PARK FAILURE: Mount did not park or it is still slewing/tracking", _equipmentOptions.MountParkTimeThresholdSeconds);
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
          "DOME CLOSE FAILURE", _equipmentOptions.DomeCloseTimeThresholdSeconds);
      _logger.LogInformation("Dome closed.");
    }

    _logger.LogInformation("Shutdown completed.");
  }

  private async Task<bool> IsMountParkedAsync()
  {
    var m = (await GetEquipmentInfoAsync())?.Response?.Mount;
    return m != null && (!m.Connected || (m.AtPark && !m.Slewing && !m.TrackingEnabled));
  }

  private async Task<bool> IsDomeClosedAsync()
  {
    var d = (await GetEquipmentInfoAsync())?.Response?.Dome;
    return d != null && (!d.Connected || d.ShutterStatus == "ShutterClosed");
  }

  private async Task<bool> IsCameraWarmingAsync()
  {
    var c = (await GetEquipmentInfoAsync())?.Response?.Camera;
    return c != null && (!c.Connected || !c.CoolerOn);
  }

  private async Task<bool> IsSequenceRunningAsync()
  {
    var s = (await GetEquipmentInfoAsync())?.Response?.Sequence;
    return s?.IsRunning ?? false;
  }

  private async Task WaitUntil(Func<Task<bool>> condition, string failureMessage, int timeoutSeconds = 60, int pollingDelay = 1000)
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
