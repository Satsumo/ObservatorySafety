
namespace ObservatorySafety.Core
{
  public interface IHttpService
  {
    Task<HttpResponseMessage> Call(HttpMethod method, string path);
  }
}