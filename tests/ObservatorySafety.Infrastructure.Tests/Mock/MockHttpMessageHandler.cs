using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ObservatorySafety.Infrastructure.Tests.Mock;

public class MockHttpMessageHandler : HttpMessageHandler
{
  public List<HttpRequestMessage> Requests { get; } = new();

  public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

  protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
  {
    Requests.Add(request);

    var response = new HttpResponseMessage(ResponseStatusCode)
    {
      RequestMessage = request
    };

    return Task.FromResult(response);
  }
}
