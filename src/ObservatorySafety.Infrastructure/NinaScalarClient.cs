using System.Net.Http;

using ObservatorySafety.Core;

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

  public Task AbortCameraExposureAsync() => Post(INinaClient.API_ABORT_CAMERA_EXPOSURE);
  public Task AbortSequenceAsync() => Post(INinaClient.API_ABORT_SEQUENCE);
  public Task StopSequenceAsync() => Post(INinaClient.API_STOP_SEQUENCE);
  public Task ParkMountAsync() => Post(INinaClient.API_PARK_MOUNT);
  public Task WarmCameraAsync() => Post(INinaClient.API_WARM_CAMERA);
  public Task CloseDomeAsync() => Post(INinaClient.API_CLOSE_DOME);

  public async Task ExecuteShutdownAsync(ShutdownCommand cmd)
  {
    if (cmd.AbortCameraExposure) await AbortCameraExposureAsync();
    if (cmd.AbortSequence) await AbortSequenceAsync();
    if (cmd.StopSequence) await StopSequenceAsync();
    if (cmd.ParkMount) await ParkMountAsync();
    if (cmd.WarmCamera) await WarmCameraAsync();
    if (cmd.CloseDome) await CloseDomeAsync();
  }

  private async Task Post(string path)
  {
    if (_dryRun)
    {
      Console.WriteLine($"[DRY-RUN] Would POST {path}");
      return;
    }

    var resp = await _http.PostAsync(path, null);
   
    resp.EnsureSuccessStatusCode();
  }
}
