using System.IO;
using System.Text;
using System.Text.Json;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Model;

namespace ObservatorySafety.Infrastructure;

public class NinaScalarClient : INinaClient
{
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

      // LOG EVERYTHING
      Console.WriteLine("=== REQUEST ===");
      Console.WriteLine("BaseAddress: " + _http.BaseAddress);
      Console.WriteLine("RequestUri: " + req.RequestUri);
      Console.WriteLine("Headers:\n" + req.Headers);
      Console.WriteLine("Content Headers:\n" + req.Content.Headers);
      Console.WriteLine("Body:\n" + await req.Content.ReadAsStringAsync());
      var resp = await _http.SendAsync(req);

      var requestContent = await req.Content.ReadAsStringAsync();

      Console.WriteLine("=== RESPONSE ===");
      Console.WriteLine("Status: " + resp.StatusCode);
      Console.WriteLine("Reason: " + resp.ReasonPhrase);
      Console.WriteLine("Response Headers:\n" + resp.Headers);
      Console.WriteLine("Response Body:\n" + await resp.Content.ReadAsStringAsync());
      
      var response = await _http.GetAsync(INinaClient.API_EQUIPMENT_INFO);

      response.EnsureSuccessStatusCode();

      var json = await response.Content.ReadAsStringAsync();
      Console.WriteLine("Deserializing JSON into EquipmentInfoEnvelope:\n" + json);
      return JsonSerializer.Deserialize<EquipmentInfoEnvelope>(json)!;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error getting equipment info: {ex.Message}");
      throw;
    }
  }

  public Task StopSequenceAsync() => Call(HttpMethod.Get, INinaClient.API_STOP_SEQUENCE);
  public Task ParkMountAsync() => Call(HttpMethod.Get, INinaClient.API_PARK_MOUNT);
  public Task WarmCameraAsync() => Call(HttpMethod.Get, INinaClient.API_WARM_CAMERA);
  public Task CloseDomeAsync() => Call(HttpMethod.Get, INinaClient.API_CLOSE_DOME);
  public async Task ExecuteShutdownAsync(ShutdownCommand cmd)
  {
    if (cmd.StopSequence)
    {
      await StopSequenceAsync();
      await WaitUntil(async () => !await IsSequenceRunningAsync(),
          "Sequence did not stop");
    }

    if (cmd.ParkMount)
    {
      await ParkMountAsync();
      await WaitUntil(async () => await IsMountParkedAsync(),
          "Mount did not park");
      await WaitUntil(async () => await IsMountIdleAsync(),
          "Mount is still slewing or tracking");
    }

    if (cmd.WarmCamera)
    {
      await WarmCameraAsync();
      await WaitUntil(async () => await IsCameraWarmingAsync(),
          "Camera did not warm");
    }

    if (cmd.CloseDome)
    {
      await CloseDomeAsync();
      await WaitUntil(async () => await IsDomeClosedAsync(),
          "Dome did not close");
    }
  }

  private async Task<bool> IsMountParkedAsync()
  {
    var info = await GetEquipmentInfoAsync();
    return info.Response?.Mount?.AtPark ?? false;
  }

  private async Task<bool> IsMountIdleAsync()
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

      // LOG EVERYTHING
      Console.WriteLine("=== REQUEST ===");
      Console.WriteLine("BaseAddress: " + _http.BaseAddress);
      Console.WriteLine("RequestUri: " + req.RequestUri);
      Console.WriteLine("Headers:\n" + req.Headers);
      Console.WriteLine("Content Headers:\n" + req.Content.Headers);
      Console.WriteLine("Body:\n" + await req.Content.ReadAsStringAsync());
      var resp = await _http.SendAsync(req);

      var requestContent = await req.Content.ReadAsStringAsync();

      Console.WriteLine("=== RESPONSE ===");
      Console.WriteLine("Status: " + resp.StatusCode);
      Console.WriteLine("Reason: " + resp.ReasonPhrase);
      Console.WriteLine("Response Headers:\n" + resp.Headers);
      Console.WriteLine("Response Body:\n" + await resp.Content.ReadAsStringAsync());

      resp.EnsureSuccessStatusCode();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error posting to base:{_http.BaseAddress} path:{path}: {ex.Message}");
      throw;
    }
  }
  private async Task WaitUntil(Func<Task<bool>> condition, string failureMessage, int timeoutSeconds = 60)
  {
    var start = DateTime.UtcNow;

    while ((DateTime.UtcNow - start).TotalSeconds < timeoutSeconds)
    {
      if (await condition())
        return;

      await Task.Delay(1000);
    }

    throw new Exception(failureMessage);
  }

}
