using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Model;


namespace ObservatorySafety.Infrastructure;

public class NinaScalarClient : INinaClient
{
  private readonly ILogger<NinaScalarClient> _logger = LogProvider.Factory.CreateLogger<NinaScalarClient>();
  private readonly HttpClient _http;
  private readonly bool _dryRun;

  public NinaScalarClient(NinaOptions options, bool dryRun, HttpMessageHandler? handler = null)
  {
    _dryRun = dryRun;

    _http = handler == null
        ? new HttpClient()
        : new HttpClient(handler);

    _http.BaseAddress = new Uri(options.BaseUrl);

    if (!string.IsNullOrWhiteSpace(options.ApiKey))
      _http.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
  }

  public async Task<EquipmentInfoEnvelope> GetEquipmentInfoAsync()
  {
    try
    {
      var req = new HttpRequestMessage(HttpMethod.Get, INinaClient.API_EQUIPMENT_INFO)
      {
        Content = new StringContent("{}", Encoding.UTF8, "application/json")
      };

      _logger.Log(LogLevel.Debug, $"=== REQUEST ===\nBaseAddress: {_http.BaseAddress
        }\nRequestUri: {req.RequestUri
        }\nHeaders:\n{req.Headers
        }\nContent Headers:\n{req.Content.Headers
        }\nBody:\n{await req.Content.ReadAsStringAsync()
        }");

      var resp = await _http.SendAsync(req);
      resp.EnsureSuccessStatusCode();

      var json = await resp.Content.ReadAsStringAsync();

      _logger.Log(LogLevel.Debug, $"=== RESPONSE ===\nStatus: {resp.StatusCode
        }\nReason: {resp.ReasonPhrase
        }\nResponse Headers:\n{resp.Headers
        }\nResponse Body:\n{await resp.Content.ReadAsStringAsync()
        }");

      return JsonSerializer.Deserialize<EquipmentInfoEnvelope>(json)!;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting equipment info!");
      throw;
    }
  }

  public Task StopSequenceAsync() => Call(HttpMethod.Get, INinaClient.API_STOP_SEQUENCE);
  public Task ParkMountAsync() => Call(HttpMethod.Get, INinaClient.API_PARK_MOUNT);
  public Task WarmCameraAsync() => Call(HttpMethod.Get, INinaClient.API_WARM_CAMERA);
  public Task CloseDomeAsync() => Call(HttpMethod.Get, INinaClient.API_CLOSE_DOME);
  public async Task ExecuteShutdownAsync(ShutdownCommand cmd)
  {
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
    return m != null && m.AtPark && !m.Slewing && !m.TrackingEnabled;
  }

  private async Task<bool> IsDomeClosedAsync()
  {
    var d = (await GetEquipmentInfoAsync()).Response?.Dome;
    return d != null && d.ShutterStatus == "ShutterClosed";
  }

  private async Task<bool> IsCameraWarmingAsync()
  {
    var c = (await GetEquipmentInfoAsync()).Response?.Camera;
    return c != null && !c.CoolerOn;
  }

  private async Task<bool> IsSequenceRunningAsync()
  {
    var s = (await GetEquipmentInfoAsync()).Response?.Sequence;
    return s?.IsRunning ?? false;
  }

  private async Task Call(HttpMethod method, string path)
  {
    if (_dryRun)
    {
      Console.WriteLine($"[DRY-RUN] Would POST {path}");
      return;
    }

    try
    {
      var req = new HttpRequestMessage(method, path)      
      {
        Content = new StringContent("{}", Encoding.UTF8, "application/json")
      };

      _logger.Log(LogLevel.Debug, $"=== REQUEST ===\nBaseAddress: {_http.BaseAddress
        }\nRequestUri: {req.RequestUri
        }\nHeaders:\n{req.Headers
        }\nContent Headers:\n{req.Content.Headers
        }\nBody:\n{await req.Content.ReadAsStringAsync()
        }");

      var resp = await _http.SendAsync(req);
      resp.EnsureSuccessStatusCode();

      var requestContent = await resp.Content.ReadAsStringAsync();

      _logger.Log(LogLevel.Debug, $"=== RESPONSE ===\nStatus: {resp.StatusCode
        }\nReason: {resp.ReasonPhrase
        }\nResponse Headers:\n{resp.Headers
        }\nResponse Body:\n{requestContent}");
      
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error {method.ToString()} to base:{_http.BaseAddress} path:{path}: {ex.Message}");
      throw;
    }
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
