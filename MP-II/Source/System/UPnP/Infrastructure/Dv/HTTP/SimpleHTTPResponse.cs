using System;
using System.IO;
using InvalidDataException=MediaPortal.Utilities.Exceptions.InvalidDataException;

namespace UPnP.Infrastructure.Dv.HTTP
{
  /// <summary>
  /// Encapsulates an HTTP response.
  /// </summary>
  /// <remarks>
  /// This class contains methods to create an HTTP response instance which can produce a byte array to be sent to
  /// the network and the other way, to parse an <see cref="SimpleHTTPResponse"/> instance from a given byte stream.
  /// </remarks>
  public class SimpleHTTPResponse : SimpleHTTPMessage
  {
    protected HTTPResponseCode _code;

    internal SimpleHTTPResponse() { }

    /// <summary>
    /// Creates a new HTTP response with the specified response <paramref name="code"/>.
    /// </summary>
    /// <param name="code">The response code to use for the created HTTP response.</param>
    public SimpleHTTPResponse(HTTPResponseCode code)
    {
      _code = code;
    }

    /// <summary>
    /// Gets the HTTP response code which was received/sets the HTTP response code to be sent.
    /// </summary>
    public HTTPResponseCode ResponseCode
    {
      get { return _code; }
      set { _code = value; }
    }

    protected static string GetResponseStr(HTTPResponseCode code)
    {
      switch (code)
      {
        case HTTPResponseCode.Continue:
          return "Continue";
        case HTTPResponseCode.SwitchingProtocols:
          return "Switching Protocols";
        case HTTPResponseCode.Ok:
          return "Ok";
        case HTTPResponseCode.Created:
          return "Created";
        case HTTPResponseCode.Accepted:
          return "Accepted";
        case HTTPResponseCode.NonAuthoritativeInformation:
          return "Non-Authoritative Information";
        case HTTPResponseCode.NoContent:
          return "No Content";
        case HTTPResponseCode.ResetContent:
          return "Reset Content";
        case HTTPResponseCode.PartialContent:
          return "Partial Content";
        case HTTPResponseCode.MultipleChoices:
          return "Multiple Choices";
        case HTTPResponseCode.MovedPermanently:
          return "Moved Permanently";
        case HTTPResponseCode.Found:
          return "Found";
        case HTTPResponseCode.SeeOther:
          return "See Other";
        case HTTPResponseCode.NotModified:
          return "Not Modified";
        case HTTPResponseCode.UseProxy:
          return "Use Proxy";
        case HTTPResponseCode.TemporaryRedirect:
          return "Temporary Redirect";
        case HTTPResponseCode.BadRequest:
          return "Bad Request";
        case HTTPResponseCode.Unauthorized:
          return "Unauthorized";
        case HTTPResponseCode.PaymentRequired:
          return "Payment Required";
        case HTTPResponseCode.Forbidden:
          return "Forbidden";
        case HTTPResponseCode.NotFound:
          return "Not Found";
        case HTTPResponseCode.MethodNotAllowed:
          return "Method Not Allowed";
        case HTTPResponseCode.NotAcceptable:
          return "Not Acceptable";
        case HTTPResponseCode.ProxyAuthenticationRequired:
          return "Proxy Authentication Required";
        case HTTPResponseCode.RequestTimeout:
          return "Request Timeout";
        case HTTPResponseCode.Conflict:
          return "Conflict";
        case HTTPResponseCode.Gone:
          return "Gone";
        case HTTPResponseCode.LengthRequired:
          return "Length Required";
        case HTTPResponseCode.PreconditionFailed:
          return "Precondition Failed";
        case HTTPResponseCode.RequestEntityTooLarge:
          return "Request Entity Too Large";
        case HTTPResponseCode.RequestURITooLong:
          return "Request-URI Too Long";
        case HTTPResponseCode.UnsupportedMediaType:
          return "Unsupported Media Type";
        case HTTPResponseCode.RequestedRangeNotSatisfiable:
          return "Requested Range Not Satisfiable";
        case HTTPResponseCode.ExpectationFailed:
          return "Expectation Failed";
        case HTTPResponseCode.InternalServerError:
          return "Internal Server Error";
        case HTTPResponseCode.NotImplemented:
          return "Not Implemented";
        case HTTPResponseCode.BadGateway:
          return "Bad Gateway";
        case HTTPResponseCode.ServiceUnavailable:
          return "Service Unavailable";
        case HTTPResponseCode.GatewayTimeout:
          return "Gateway Timeout";
        case HTTPResponseCode.HTTPVersionNotSupported:
          return "HTTP Version Not Supported";
      }
      throw new NotImplementedException(string.Format("HTTP response code '{0}' is not implemented", code));
    }

    protected override string EncodeStartingLine()
    {
      return string.Format("{0} {1} {2}", _httpVersion, (int) _code, GetResponseStr(_code));
    }

    /// <summary>
    /// Parses the HTTP request out of the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">HTTP data stream to parse.</param>
    /// <param name="result">Returns the parsed HTTP response instance.</param>
    /// <exception cref="MediaPortal.Utilities.Exceptions.InvalidDataException">If the given <paramref name="stream"/> is malformed.</exception>
    public static void Parse(Stream stream, out SimpleHTTPResponse result)
    {
      result = new SimpleHTTPResponse();
      string firstLine;
      result.ParseHeaderAndBody(stream, out firstLine);
      string[] elements = firstLine.Split(' ');
      if (elements.Length != 3)
        throw new InvalidDataException("Invalid HTTP request header starting line '{0}'", firstLine);
      string httpVersion = elements[0];
      if (httpVersion != "HTTP/1.0" && httpVersion != "HTTP/1.1")
        throw new InvalidDataException("Invalid HTTP request header starting line '{0}'", firstLine);
      int code;
      if (!int.TryParse(elements[1], out code))
        throw new InvalidDataException("Invalid HTTP request header starting line '{0}'", firstLine);
      result._code = (HTTPResponseCode) code;
    }
  }
}
