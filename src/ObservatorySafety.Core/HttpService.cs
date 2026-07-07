using System.Text;

using Microsoft.Extensions.Logging;

namespace ObservatorySafety.Core
{
  public class HttpService
  {
    private readonly ILogger<HttpService> _logger = LogProvider.Factory.CreateLogger<HttpService>();
    private readonly HttpClient _http;

    public HttpService(string baseUrl, string? apiKey, HttpMessageHandler? handler = null) {
      _http = handler == null
          ? new HttpClient()
          : new HttpClient(handler);

      _http.BaseAddress = new Uri(baseUrl);

      if (!string.IsNullOrWhiteSpace(apiKey))
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task<HttpResponseMessage> Call(HttpMethod method, string path)
    {
      
      var req = new HttpRequestMessage(method, path)
      {
        Content = new StringContent("{}", Encoding.UTF8, "application/json")
      };

      _logger.Log(LogLevel.Debug, $"=== REQUEST ===\nBaseAddress: {_http.BaseAddress}\n" +
        $"RequestUri: {req.RequestUri}\n" +
        $"Headers:\n{req.Headers}\n" +
        $"Content Headers:\n{req.Content.Headers}\n" +
        $"Body:\n{await req.Content.ReadAsStringAsync()}");

      var resp = await _http.SendAsync(req);
      resp.EnsureSuccessStatusCode();

      var requestContent = await resp.Content.ReadAsStringAsync();

      _logger.Log(LogLevel.Debug, $"=== RESPONSE ===\nStatus: {resp.StatusCode}\n" +
        $"Reason: {resp.ReasonPhrase}\n" +
        $"Response Headers:\n{resp.Headers}\n" +
        $"Response Body:\n{requestContent}");

      return resp;
    }
  }
}
