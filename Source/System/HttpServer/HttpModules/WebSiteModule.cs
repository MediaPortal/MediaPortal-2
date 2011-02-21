using System;
using System.Collections.Generic;
using System.Net;
using HttpServer.Sessions;

namespace HttpServer.HttpModules
{
  /// <summary>
  /// The website module let's you handle multiple websites in the same server.
  /// It uses the "Host" header to check which site you want.
  /// </summary>
  /// <remarks>It's recommended that you do not
  /// add any other modules to HttpServer if you are using the website module. Instead,
  /// add all wanted modules to each website.</remarks>
  internal class WebSiteModule : HttpModule
  {
    private readonly string _host;
    private readonly List<HttpModule> _modules = new List<HttpModule>();
    private readonly string _siteName;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">domain name that should be handled.</param>
    /// <param name="name"></param>
    public WebSiteModule(string host, string name)
    {
      _host = host;
      _siteName = name;
    }

    /// <summary>
    /// Name of site.
    /// </summary>
    public string SiteName
    {
      get { return _siteName; }
    }

    public bool CanHandle(Uri uri)
    {
      return string.Compare(uri.Host, _host, true) == 0;
    }

    /// <summary>
    /// Method that process the url
    /// </summary>
    /// <param name="request">Information sent by the browser about the request</param>
    /// <param name="response">Information that is being sent back to the client.</param>
    /// <param name="session">Session used to </param>
    public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      if (!CanHandle(request.Uri))
        return false;

      bool handled = false;
      foreach (HttpModule module in _modules)
      {
        if (module.Process(request, response, session))
          handled = true;
      }

      if (!handled)
        response.Status = HttpStatusCode.NotFound;

      return true;
    }
  }
}