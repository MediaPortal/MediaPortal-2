using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.WebRequests
{
  public class CompressionWebClient : WebClient
  {
    protected bool _enableCompression;
    protected int _requestTimeOut = 20000;

    public int RequestTimeout
    {
      get { return _requestTimeOut; }
      set { _requestTimeOut = value; }
    }

    public CompressionWebClient()
      : this(true)
    {
    }

    public CompressionWebClient(bool enableCompression)
    {
      _enableCompression = enableCompression;
    }

    protected override WebRequest GetWebRequest(Uri address)
    {
      HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
      if (request != null && _enableCompression)
      {
        Headers["Accept-Encoding"] = "gzip, deflate";
        request.Timeout = RequestTimeout; // Use 20 seconds - default is 100 seconds
        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
      }
      return request;
    }
  }
}
