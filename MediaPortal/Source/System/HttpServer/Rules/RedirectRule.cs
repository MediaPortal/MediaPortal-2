using System;

namespace HttpServer.Rules
{
  /// <summary>
  /// redirects from one URL to another.
  /// </summary>
  public class RedirectRule : IRule
  {
    private readonly string _fromUrl;
    private readonly string _toUrl;
    private readonly bool _shouldRedirect = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectRule"/> class.
    /// </summary>
    /// <param name="fromUrl">Absolute path (no server name)</param>
    /// <param name="toUrl">Absolute path (no server name)</param>
    /// <example>
    /// server.Add(new RedirectRule("/", "/user/index"));
    /// </example>
    public RedirectRule(string fromUrl, string toUrl)
    {
      _fromUrl = fromUrl;
      _toUrl = toUrl;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectRule"/> class.
    /// </summary>
    /// <param name="fromUrl">Absolute path (no server name)</param>
    /// <param name="toUrl">Absolute path (no server name)</param>
    /// <param name="shouldRedirect">true if request should be redirected, false if the request URI should be replaced.</param>
    /// <example>
    /// server.Add(new RedirectRule("/", "/user/index"));
    /// </example>
    public RedirectRule(string fromUrl, string toUrl, bool shouldRedirect)
    {
      _fromUrl = fromUrl;
      _toUrl = toUrl;
      _shouldRedirect = shouldRedirect;
    }

    /// <summary>
    /// Gets string to match request URI with.
    /// </summary>
    /// <remarks>Is compared to request.Uri.AbsolutePath</remarks>
    public string FromUrl
    {
      get { return _fromUrl; }
    }

    /// <summary>
    /// Gets where to redirect.
    /// </summary>
    public string ToUrl
    {
      get { return _toUrl; }
    }

    /// <summary>
    /// Gets whether server should redirect client.
    /// </summary>
    /// <remarks>
    /// <c>false</c> means that the rule will replace
    /// the current request URI with the new one from this class.
    /// <c>true</c> means that a redirect response is sent to the client.
    /// </remarks>
    public bool ShouldRedirect
    {
      get { return _shouldRedirect; }
    }

    /// <summary>
    /// Process the incoming request.
    /// </summary>
    /// <param name="request">incoming HTTP request</param>
    /// <param name="response">outgoing HTTP response</param>
    /// <returns>true if response should be sent to the browser directly (no other rules or modules will be processed).</returns>
    /// <remarks>
    /// returning true means that no modules will get the request. Returning true is typically being done
    /// for redirects.
    /// </remarks>
    public virtual bool Process(IHttpRequest request, IHttpResponse response)
    {
      if (request.Uri.AbsolutePath == FromUrl)
      {
        if (!ShouldRedirect)
        {
          request.Uri = new Uri(request.Uri, ToUrl);
          return false;
        }

        response.Redirect(ToUrl);
        return true;
      }

      return false;
    }
  }
}