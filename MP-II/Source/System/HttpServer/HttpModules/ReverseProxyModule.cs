using System;
using System.IO;
using System.Net;
using HttpServer.Exceptions;
using HttpServer.Sessions;

namespace HttpServer.HttpModules
{
  /// <summary>
  /// A reverse proxy are used to act as a bridge between local (protected/hidden) websites
  /// and public clients.
  /// 
  /// A typical usage is to allow web servers on non standard ports to still be available
  /// to the public clients, or allow web servers on private ips to be available.
  /// </summary>
  public class ReverseProxyModule : HttpModule
  {
    private readonly string _destinationUrl;
    private readonly string _sourceUrl;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source">Base url requested from browser</param>
    /// <param name="destination">Base url on private web server</param>
    /// <example>
    /// // this will return contents from http://192.168.1.128/view/jonas when client requests http://www.gauffin.com/user/view/jonas
    /// _server.Add(new ReverseProxyModule("http://www.gauffin.com/user/", "http://192.168.1.128/");
    /// </example>
    public ReverseProxyModule(string source, string destination)
    {
      if (destination == null)
        throw new ArgumentNullException("destination");
      if (source == null)
        throw new ArgumentNullException("source");

      _destinationUrl = destination;
      if (!_destinationUrl.EndsWith("/"))
        _destinationUrl += "/";
      _sourceUrl = source;
    }

    /// <summary>
    /// Method that determines if an url should be handled or not by the module
    /// </summary>
    /// <param name="uri">Url requested by the client.</param>
    /// <returns>true if module should handle the url.</returns>
    private bool CanHandle(Uri uri)
    {
      return uri.AbsolutePath.StartsWith(_sourceUrl);
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

      int pos = request.Uri.OriginalString.IndexOf(_sourceUrl);
      if (pos == -1)
        throw new InternalServerException("Failed to find source url '" + _sourceUrl + "' for proxy.");

      string sourceUrl = request.Uri.OriginalString.Substring(0, pos + _sourceUrl.Length);
      string newUrl = request.Uri.OriginalString.Replace(sourceUrl, _destinationUrl);
      Uri url = new Uri(newUrl);

      WebClient client = new WebClient();
      try
      {
        byte[] bytes = client.DownloadData(url);
        string content = client.ResponseHeaders[HttpResponseHeader.ContentType];
        response.ContentType = content;

        string contentLengthStr = client.ResponseHeaders[HttpResponseHeader.ContentLength];
        int contentLength;
        int.TryParse(contentLengthStr, out contentLength);
        response.ContentLength = contentLength;

        if (content.StartsWith("text/html"))
        {
          string data = client.Encoding.GetString(bytes);
          StreamWriter writer = new StreamWriter(response.Body, client.Encoding);
          writer.Write(data.Replace(_destinationUrl, sourceUrl).Replace("href=\"/", "href=\"" + _sourceUrl));
          writer.Write(data.Replace(_destinationUrl, sourceUrl).Replace("src=\"/", "src=\"" + _sourceUrl));
        }
        else
          response.Body.Write(bytes, 0, bytes.Length);
      }
      catch (WebException err)
      {
        throw new InternalServerException("Failed to proxy " + url, err);
      }
      catch (NotSupportedException err)
      {
        throw new InternalServerException("Failed to proxy " + url, err);
      }
      catch (IOException err)
      {
        throw new InternalServerException("Failed to proxy " + url, err);
      }
      catch (FormatException err)
      {
        throw new InternalServerException("Failed to proxy " + url, err);
      }
      catch (ObjectDisposedException err)
      {
        throw new InternalServerException("Failed to proxy " + url, err);
      }
      catch (ArgumentNullException err)
      {
        throw new InternalServerException("Failed to proxy " + url, err);
      }

      return true;
    }
  }
}