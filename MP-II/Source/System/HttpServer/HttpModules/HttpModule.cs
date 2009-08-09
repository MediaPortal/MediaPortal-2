using HttpServer.Authentication;
using HttpServer.Exceptions;
using HttpServer.Sessions;

namespace HttpServer.HttpModules
{
  /// <summary>
  /// A HttpModule can be used to serve Uri's. The module itself
  /// decides if it should serve a Uri or not. In this way, you can
  /// get a very flexible http application since you can let multiple modules
  /// serve almost similar urls.
  /// </summary>
  /// <remarks>
  /// Throw <see cref="UnauthorizedException"/> if you are using a <see cref="AuthenticationModule"/> and want to prompt for user name/password.
  /// </remarks>
  public abstract class HttpModule
  {
    private ILogWriter _log = NullLogWriter.Instance;

    /// <summary>
    /// Method that process the url
    /// </summary>
    /// <param name="request">Information sent by the browser about the request</param>
    /// <param name="response">Information that is being sent back to the client.</param>
    /// <param name="session">Session used to </param>
    /// <returns>true if this module handled the request.</returns>
    public abstract bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session);

    /*
    /// <summary>
    /// Checks if authentication is required by the module.
    /// </summary>
    /// <param name="request">Information sent by the browser about the request</param>
    /// <param name="response">Information that is being sent back to the client.</param>
    /// <param name="session">Session used to </param>
    /// <param name="cookies">Incoming/outgoing cookies. If you modify a cookie, make sure that you also set a expire date. Modified cookies will automatically be sent.</param>
    /// <returns>true authentication should be used.</returns>
    public abstract bool IsAuthenticationRequired(IHttpRequest request, IHttpResponse response, IHttpSession session,
        HttpCookies cookies);
    */

    /// <summary>
    /// Set the log writer to use.
    /// </summary>
    /// <param name="writer">logwriter to use.</param>
    public void SetLogWriter(ILogWriter writer)
    {
      _log = writer ?? NullLogWriter.Instance;
    }

    /// <summary>
    /// Log something.
    /// </summary>
    /// <param name="prio">importance of log message</param>
    /// <param name="message">message</param>
    protected virtual void Write(LogPrio prio, string message)
    {
      _log.Write(this, prio, message);
    }

    /// <summary>
    /// If true specifies that the module doesn't consume the processing of a request so that subsequent modules
    /// can continue processing afterwards. Default is false.
    /// </summary>
    public virtual bool AllowSecondaryProcessing
    {
      get { return false; }
    }
  }
}