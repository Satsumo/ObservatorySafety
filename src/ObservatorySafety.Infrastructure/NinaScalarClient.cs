using System.Net.Http;

using ObservatorySafety.Core;

namespace ObservatorySafety.Infrastructure;

public class NinaScalarClient : INinaClient
{
  private readonly HttpClient _http;

  public NinaScalarClient(NinaOptions options, HttpMessageHandler? handler = null)
  {
    _http = handler == null
        ? new HttpClient()
        : new HttpClient(handler);

    _http.BaseAddress = new Uri(options.BaseUrl);

    if (!string.IsNullOrWhiteSpace(options.ApiKey))
      _http.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
  }

  public Task AbortSequenceAsync()
      => Post("/api/v1/sequences/abort");

  public Task StopSequenceAsync()
      => Post("/api/v1/sequences/stop");

  public Task ParkMountAsync()
      => Post("/api/v1/mount/park");

  public Task WarmCameraAsync()
      => Post("/api/v1/camera/warm");

  public Task CloseDomeAsync()
      => Post("/api/v1/dome/close");

  private async Task Post(string path)
  {
    var resp = await _http.PostAsync(path, null);
    resp.EnsureSuccessStatusCode();
  }
}
