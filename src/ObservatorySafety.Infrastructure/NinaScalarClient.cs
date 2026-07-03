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

  public Task AbortSequenceAsync() => Post("/api/v1/sequences/abort");
  public Task StopSequenceAsync() => Post("/api/v1/sequences/stop");
  public Task ParkMountAsync() => Post("/api/v1/mount/park");
  public Task WarmCameraAsync() => Post("/api/v1/camera/warm");
  public Task CloseDomeAsync() => Post("/api/v1/dome/close");

  public async Task ExecuteShutdownAsync(ShutdownCommand cmd)
  {
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
