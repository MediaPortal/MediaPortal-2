using System.Net;

namespace UPnP.Infrastructure.CP
{
  public class AsyncWebRequestState
  {
    protected HttpWebRequest _httpWebRequest;

    public AsyncWebRequestState(HttpWebRequest request)
    {
      _httpWebRequest = request;
    }

    public HttpWebRequest Request
    {
      get { return _httpWebRequest; }
    }
  }
}