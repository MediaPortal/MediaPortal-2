using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace MediaPortal.UiComponents.News
{
  public class CompressionWebClient : WebClient
  {
    protected override WebRequest GetWebRequest(Uri address)
    {
      Headers["Accept-Encoding"] = "gzip,deflate";
      HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
      request.Timeout = 20000; // use 20 seconds - default is 100 seconds
      request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
      return request;
    }
  }
}
