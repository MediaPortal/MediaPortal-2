using System;
using System.Web;

namespace HttpServer
{
  /// <summary>
  /// cookie being sent back to the browser.
  /// </summary>
  /// <seealso cref="ResponseCookie"/>
  public class ResponseCookie : RequestCookie
  {
    private const string _nullPath = "/";
    private bool _persistant = false;
    private DateTime _expires;
    private string _path = "/";
    private readonly string _domain;

    #region constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="id">cookie identifier</param>
    /// <param name="content">cookie content</param>
    /// <param name="expiresAt">cookie expiration date. Use DateTime.MinValue for session cookie.</param>
    /// <exception cref="ArgumentNullException">id or content is null</exception>
    /// <exception cref="ArgumentException">id is empty</exception>
    public ResponseCookie(string id, string content, DateTime expiresAt)
        : base(id, content)
    {
      if (expiresAt != DateTime.MinValue)
      {
        _expires = expiresAt;
        _persistant = true;
      }
    }

    /// <summary>
    /// Create a new cookie
    /// </summary>
    /// <param name="name">name identifying the cookie</param>
    /// <param name="value">cookie value</param>
    /// <param name="expires">when the cookie expires. Setting DateTime.MinValue will delete the cookie when the session is closed.</param>
    /// <param name="path">Path to where the cookie is valid</param>
    /// <param name="domain">Domain that the cookie is valid for.</param>
    public ResponseCookie(string name, string value, DateTime expires, string path, string domain)
        : this(name, value, expires)
    {
      _domain = domain;
      _path = path;
    }

    /// <summary>
    /// Create a new cookie
    /// </summary>
    /// <param name="cookie">Name and value will be used</param>
    /// <param name="expires">when the cookie expires.</param>
    public ResponseCookie(RequestCookie cookie, DateTime expires)
        : this(cookie.Name, cookie.Value, expires)
    {
    }

    #endregion

    #region inherited methods

    /// <summary>
    /// Gets the cookie HTML representation.
    /// </summary>
    /// <returns>cookie string</returns>
    public override string ToString()
    {
      string temp = string.Format("{0}={1}; ", HttpUtility.UrlEncode(Name), HttpUtility.UrlEncode(Value));
      if (_persistant)
      {
        TimeSpan span = DateTime.Now - DateTime.UtcNow;
        DateTime utc = _expires.Subtract(span);
        temp += string.Format("expires={0};", utc.ToString("r"));
      }
      if (!string.IsNullOrEmpty(_path))
        temp += string.Format("path={0}; ", _path);
      if (!string.IsNullOrEmpty(_domain))
        temp += string.Format("domain={0}; ", _domain);

      return temp;
    }

    #endregion

    #region public properties

    /// <summary>
    /// When the cookie expires.
    /// DateTime.MinValue means that the cookie expires when the session do so.
    /// </summary>
    public DateTime Expires
    {
      get { return _expires; }
      set
      {
        _expires = value;
        _persistant = value != DateTime.MinValue;
      }
    }

    /// <summary>
    /// Cookie is only valid under this path.
    /// </summary>
    public string Path
    {
      get { return _path; }
      set
      {
        if (!string.IsNullOrEmpty(value))
          _path = value;
        else
          _path = _nullPath;
      }
    }

    #endregion
  }
}