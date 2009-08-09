using System;

namespace HttpServer.Parser
{
  /// <summary>
  /// Used when the request line have been successfully parsed.
  /// </summary>
  public class RequestLineEventArgs : EventArgs
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLineEventArgs"/> class.
    /// </summary>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <param name="uriPath">The URI path.</param>
    /// <param name="httpVersion">The HTTP version.</param>
    public RequestLineEventArgs(string httpMethod, string uriPath, string httpVersion)
    {
      HttpMethod = httpMethod;
      UriPath = uriPath;
      HttpVersion = httpVersion;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLineEventArgs"/> class.
    /// </summary>
    public RequestLineEventArgs()
    {
    }

    /// <summary>
    /// Gets or sets http method.
    /// </summary>
    /// <remarks>
    /// Should be one of the methods declared in <see cref="Method"/>.
    /// </remarks>
    public string HttpMethod { get; set; }

    /// <summary>
    /// Gets or sets the version of the HTTP protocol that the client want to use.
    /// </summary>
    public string HttpVersion { get; set; }

    /// <summary>
    /// Gets or sets requested URI path.
    /// </summary>
    public string UriPath { get; set; }
  }
}