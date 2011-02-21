using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using HttpServer.Exceptions;
using HttpServer.FormDecoders;

namespace HttpServer
{
  /// <summary>
  /// Contains server side HTTP request information.
  /// </summary>
  public class HttpRequest : IHttpRequest
  {
    /// <summary>
    /// Chars used to split an URL path into multiple parts.
    /// </summary>
    public static readonly char[] UriSplitters = new[] {'/'};
    private readonly NameValueCollection _headers = new NameValueCollection();
    private readonly HttpParam _param = new HttpParam(HttpInput.Empty, HttpInput.Empty);
    private Stream _body = new MemoryStream();
    private int _bodyBytesLeft;
    private ConnectionType _connection = ConnectionType.Close;
    private int _contentLength;
    private HttpForm _form = HttpForm.EmptyForm;
    private string _httpVersion = string.Empty;
    private string _method = string.Empty;
    private HttpInput _queryString = HttpInput.Empty;
    private Uri _uri = HttpHelper.EmptyUri;
    private string _uriPath;

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="HttpRequest"/> is secure.
    /// </summary>
    public bool Secure { get; internal set; }

    /// <summary>
    /// Path and query (will be merged with the host header) and put in Uri
    /// </summary>
    /// <see cref="Uri"/>
    public string UriPath
    {
      get { return _uriPath; }
      set
      {
        _uriPath = value;
        int pos = _uriPath.IndexOf('?');
        if (pos != -1)
        {
          _queryString = HttpHelper.ParseQueryString(_uriPath.Substring(pos + 1));
          _param.SetQueryString(_queryString);
          string path = _uriPath.Substring(0, pos);
          _uriPath = System.Web.HttpUtility.UrlDecode(path) + "?" + _uriPath.Substring(pos + 1);
          UriParts = value.Substring(0, pos).Split(UriSplitters, StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
          _uriPath = System.Web.HttpUtility.UrlDecode(_uriPath);
          UriParts = value.Split(UriSplitters, StringSplitOptions.RemoveEmptyEntries);
        }
      }
    }

    /// <summary>
    /// Assign a form.
    /// </summary>
    /// <param name="form"></param>
    internal void AssignForm(HttpForm form)
    {
      _form = form;
    }

    internal static bool ShouldReplyTo100Continue(IHttpRequest request)
    {
      string expectHeader = request.Headers["expect"];
      return expectHeader != null && expectHeader.Contains("100-continue");
    }

    #region IHttpRequest Members

    /// <summary>
    /// Gets whether the body is complete.
    /// </summary>
    public bool BodyIsComplete
    {
      get { return _bodyBytesLeft == 0; }
    }

    /// <summary>
    /// Gets kind of types accepted by the client.
    /// </summary>
    public string[] AcceptTypes { get; private set; }

    /// <summary>
    /// Gets or sets body stream.
    /// </summary>
    public Stream Body
    {
      get { return _body; }
      set { _body = value; }
    }

    /// <summary>
    /// Gets or sets kind of connection used for the session.
    /// </summary>
    public ConnectionType Connection
    {
      get { return _connection; }
      set { _connection = value; }
    }

    /// <summary>
    /// Gets or sets number of bytes in the body.
    /// </summary>
    public int ContentLength
    {
      get { return _contentLength; }
      set
      {
        _contentLength = value;
        _bodyBytesLeft = value;
      }
    }

    /// <summary>
    /// Gets headers sent by the client.
    /// </summary>
    public NameValueCollection Headers
    {
      get { return _headers; }
    }

    /// <summary>
    /// Gets or sets version of HTTP protocol that's used.
    /// </summary>
    /// <remarks>
    /// Probably <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/>.
    /// </remarks>
    /// <seealso cref="HttpHelper"/>
    public string HttpVersion
    {
      get { return _httpVersion; }
      set { _httpVersion = value; }
    }


    /// <summary>
    /// Gets or sets requested method.
    /// </summary>
    /// <value></value>
    /// <remarks>
    /// Will always be in upper case.
    /// </remarks>
    /// <see cref="HttpServer.Method"/>
    public string Method
    {
      get { return _method; }
      set { _method = value; }
    }

    /// <summary>
    /// Gets variables sent in the query string
    /// </summary>
    public HttpInput QueryString
    {
      get { return _queryString; }
    }


    /// <summary>
    /// Gets or sets requested URI.
    /// </summary>
    public Uri Uri
    {
      get { return _uri; }
      set
      {
        _uri = value ?? HttpHelper.EmptyUri;
        UriParts = _uri.AbsolutePath.Split(UriSplitters, StringSplitOptions.RemoveEmptyEntries);
      }
    }

    /// <summary>
    /// Uri absolute path splitted into parts.
    /// </summary>
    /// <example>
    /// // uri is: http://gauffin.com/code/tiny/
    /// Console.WriteLine(request.UriParts[0]); // result: code
    /// Console.WriteLine(request.UriParts[1]); // result: tiny
    /// </example>
    /// <remarks>
    /// If you're using controllers than the first part is controller name,
    /// the second part is method name and the third part is Id property.
    /// </remarks>
    /// <seealso cref="Uri"/>
    public string[] UriParts { get; private set; }

    /// <summary>
    /// Gets parameter from <see cref="QueryString"/> or <see cref="Form"/>.
    /// </summary>
    public HttpParam Param
    {
      get { return _param; }
    }

    /// <summary>
    /// Gets form parameters.
    /// </summary>
    public HttpForm Form
    {
      get { return _form; }
    }

    /// <summary>
    /// Gets whether the request was made by Ajax (Asynchronous JavaScript)
    /// </summary>
    public bool IsAjax { get; private set; }

    /// <summary>
    /// Gets cookies that was sent with the request.
    /// </summary>
    public RequestCookies Cookies { get; private set; }

    ///<summary>
    ///Creates a new object that is a copy of the current instance.
    ///</summary>
    ///
    ///<returns>
    ///A new object that is a copy of this instance.
    ///</returns>
    ///<filterpriority>2</filterpriority>
    public object Clone()
    {
      // this method was mainly created for testing.
      // dont use it that much...
      var request = new HttpRequest();
      request.Method = _method;
      if (AcceptTypes != null)
      {
        request.AcceptTypes = new string[AcceptTypes.Length];
        AcceptTypes.CopyTo(request.AcceptTypes, 0);
      }
      request._httpVersion = _httpVersion;
      request._queryString = _queryString;
      request.Uri = _uri;

      var buffer = new byte[_body.Length];
      _body.Read(buffer, 0, (int) _body.Length);
      request.Body = new MemoryStream();
      request.Body.Write(buffer, 0, buffer.Length);
      request.Body.Seek(0, SeekOrigin.Begin);
      request.Body.Flush();

      request._headers.Clear();
      foreach (string key in _headers)
      {
        string[] values = _headers.GetValues(key);
        if (values != null)
          foreach (string value in values)
            request.AddHeader(key, value);
      }
      Clear();
      return request;
    }

    /// <summary>
    /// Decode body into a form.
    /// </summary>
    /// <param name="providers">A list with form decoders.</param>
    /// <exception cref="InvalidDataException">If body contents is not valid for the chosen decoder.</exception>
    /// <exception cref="InvalidOperationException">If body is still being transferred.</exception>
    public void DecodeBody(FormDecoderProvider providers)
    {
      if (_bodyBytesLeft > 0)
        throw new InvalidOperationException("Body have not yet been completed.");

      _form = providers.Decode(_headers["content-type"], _body, Encoding.UTF8);
      if (_form != HttpInput.Empty)
        _param.SetForm(_form);
    }

    ///<summary>
    /// Cookies
    ///</summary>
    ///<param name="cookies">the cookies</param>
    public void SetCookies(RequestCookies cookies)
    {
      Cookies = cookies;
    }

    /// <summary>
    /// Create a response object.
    /// </summary>
    /// <returns>A new <see cref="IHttpResponse"/>.</returns>
    public IHttpResponse CreateResponse(IHttpClientContext context)
    {
      return new HttpResponse(context, this);
    }

    /// <summary>
    /// Called during parsing of a <see cref="IHttpRequest"/>.
    /// </summary>
    /// <param name="name">Name of the header, should not be URL encoded</param>
    /// <param name="value">Value of the header, should not be URL encoded</param>
    /// <exception cref="BadRequestException">If a header is incorrect.</exception>
    public void AddHeader(string name, string value)
    {
      if (string.IsNullOrEmpty(name))
        throw new BadRequestException("Invalid header name: " + name ?? "<null>");
      if (string.IsNullOrEmpty(value))
        throw new BadRequestException("Header '" + name + "' do not contain a value.");

      switch (name.ToLower())
      {
        case "http_x_requested_with":
        case "x-requested-with":
          if (string.Compare(value, "XMLHttpRequest", true) == 0)
            IsAjax = true;
          break;
        case "accept":
          AcceptTypes = value.Split(',');
          for (int i = 0; i < AcceptTypes.Length; ++i)
            AcceptTypes[i] = AcceptTypes[i].Trim();
          break;
        case "content-length":
          int t;
          if (!int.TryParse(value, out t))
            throw new BadRequestException("Invalid content length.");
          ContentLength = t;
          break; //todo: maybe throw an exception
        case "host":
          try
          {
            _uri = new Uri(Secure ? "https://" : "http://" + value + _uriPath);
            UriParts = _uri.AbsolutePath.Split(UriSplitters, StringSplitOptions.RemoveEmptyEntries);
          }
          catch (UriFormatException err)
          {
            throw new BadRequestException("Failed to parse uri: " + value + _uriPath, err);
          }
          break;
        case "remote_addr":
          // to prevent hacking (since it's added by IHttpClientContext before parsing).
          if (_headers[name] == null)
            _headers.Add(name, value);
          break;

        case "connection":
          if (string.Compare(value, "close", true) == 0)
            Connection = ConnectionType.Close;
          else if (value.StartsWith("keep-alive", StringComparison.CurrentCultureIgnoreCase))
            Connection = ConnectionType.KeepAlive;
          else
            throw new BadRequestException("Unknown 'Connection' header type.");
          break;

        case "expect":
          if (value.Contains("100-continue"))
          {
          }
          _headers.Add(name, value);
          break;

        default:
          _headers.Add(name, value);
          break;
      }
    }

    /// <summary>
    /// Add bytes to the body
    /// </summary>
    /// <param name="bytes">buffer to read bytes from</param>
    /// <param name="offset">where to start read</param>
    /// <param name="length">number of bytes to read</param>
    /// <returns>Number of bytes actually read (same as length unless we got all body bytes).</returns>
    /// <exception cref="InvalidOperationException">If body is not writable</exception>
    /// <exception cref="ArgumentNullException"><c>bytes</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><c>offset</c> is out of range.</exception>
    public int AddToBody(byte[] bytes, int offset, int length)
    {
      if (bytes == null)
        throw new ArgumentNullException("bytes");
      if (offset + length > bytes.Length)
        throw new ArgumentOutOfRangeException("offset");
      if (length == 0)
        return 0;
      if (!_body.CanWrite)
        throw new InvalidOperationException("Body is not writable.");

      if (length > _bodyBytesLeft)
      {
        length = _bodyBytesLeft;
      }

      _body.Write(bytes, offset, length);
      _bodyBytesLeft -= length;

      return length;
    }

    /// <summary>
    /// Clear everything in the request
    /// </summary>
    public void Clear()
    {
      _body.Dispose();
      _body = new MemoryStream();
      _contentLength = 0;
      _method = string.Empty;
      _uri = HttpHelper.EmptyUri;
      _queryString = HttpInput.Empty;
      _bodyBytesLeft = 0;
      _headers.Clear();
      _connection = ConnectionType.Close;
      IsAjax = false;
      _form.Clear();
    }

    #endregion
  }
}