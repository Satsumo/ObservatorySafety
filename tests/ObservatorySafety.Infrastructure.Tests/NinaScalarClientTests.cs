using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure.Tests.Mock;

namespace ObservatorySafety.Infrastructure.Tests;

[TestFixture]
public class NinaScalarClientTests
{
  [Test]
  public async Task CallsCorrectEndpoints_ForShutdown()
  {
    var handler = new MockHttpMessageHandler();
    var httpService = new HttpService("http://localhost:1888", null, handler);
    var client = new NinaScalarClient(httpService);

    await client.StopSequenceAsync();
    await client.ParkMountAsync();
    await client.WarmCameraAsync();
    await client.CloseDomeAsync();

    var paths = handler.Requests.Select(r => r.RequestUri!.AbsolutePath).ToList();

    Assert.That(paths, Has.Member("/v2/api/sequence/stop"));
    Assert.That(paths, Has.Member("/v2/api/equipment/mount/park"));
    Assert.That(paths, Has.Member("/v2/api/equipment/camera/warm"));
    Assert.That(paths, Has.Member("/v2/api/equipment/dome/close"));
  }

  [Test]
  public void ThrowsOnNonSuccessStatusCode()
  {
    var handler = new MockHttpMessageHandler { ResponseStatusCode = System.Net.HttpStatusCode.BadRequest };
    var httpService = new HttpService("http://localhost:1888", null, handler);
    var client = new NinaScalarClient(httpService);

    Assert.ThrowsAsync<HttpRequestException>(() => client.StopSequenceAsync());
  }
}
