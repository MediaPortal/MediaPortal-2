using System;
using HttpServer.Exceptions;

namespace HttpServer.Authentication
{
  /// <summary>
  /// Delegate used to let authentication modules authenticate the user name and password.
  /// </summary>
  /// <param name="realm">Realm that the user want to authenticate in</param>
  /// <param name="userName">User name specified by client</param>
  /// <param name="token">Can either be user password or implementation specific token.</param>
  /// <param name="login">object that will be stored in a session variable called <see cref="AuthenticationModule.AuthenticationTag"/> if authentication was successful.</param>
  /// <exception cref="ForbiddenException">throw forbidden exception if too many attempts have been made.</exception>
  /// <remarks>
  /// <para>
  /// Use <see cref="DigestAuthentication.TokenIsHA1"/> to specify that the token is a HA1 token. (MD5 generated
  /// string from realm, user name and password); Md5String(userName + ":" + realm + ":" + password);
  /// </para>
  /// </remarks>
  public delegate void AuthenticationHandler(string realm, string userName, ref string token, out object login);

  /// <summary>
  /// Let's you decide on a system level if authentication is required.
  /// </summary>
  /// <param name="request">HTTP request from client</param>
  /// <returns>true if user should be authenticated.</returns>
  /// <remarks>throw <see cref="ForbiddenException"/> if no more attempts are allowed.</remarks>
  /// <exception cref="ForbiddenException">If no more attempts are allowed</exception>
  public delegate bool AuthenticationRequiredHandler(IHttpRequest request);

  /// <summary>
  /// Authentication modules are used to implement different
  /// kind of HTTP authentication.
  /// </summary>
  public abstract class AuthenticationModule
  {
    private readonly AuthenticationHandler _authenticator;
    private readonly AuthenticationRequiredHandler _authenticationRequiredHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationModule"/> class.
    /// </summary>
    /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
    /// <param name="authenticationRequiredHandler">Delegate used to determine if authentication is required (may be null).</param>
    protected AuthenticationModule(
        AuthenticationHandler authenticator, AuthenticationRequiredHandler authenticationRequiredHandler)
    {
      Check.Require(authenticator, "authenticator");
      _authenticationRequiredHandler = authenticationRequiredHandler;
      _authenticator = authenticator;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationModule"/> class.
    /// </summary>
    /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
    protected AuthenticationModule(AuthenticationHandler authenticator) : this(authenticator, null)
    {
    }

    /// <summary>
    /// name used in HTTP request.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Tag used for authentication.
    /// </summary>
    public const string AuthenticationTag = "__authtag";

    /// <summary>
    /// Create a response that can be sent in the WWW-Authenticate header.
    /// </summary>
    /// <param name="realm">Realm that the user should authenticate in</param>
    /// <param name="options">Array with optional options.</param>
    /// <returns>A correct authentication request.</returns>
    /// <exception cref="ArgumentNullException">If realm is empty or null.</exception>
    public abstract string CreateResponse(string realm, params object[] options);

    /// <summary>
    /// An authentication response have been received from the web browser.
    /// Check if it's correct
    /// </summary>
    /// <param name="authenticationHeader">Contents from the Authorization header</param>
    /// <param name="realm">Realm that should be authenticated</param>
    /// <param name="httpVerb">GET/POST/PUT/DELETE etc.</param>
    /// <param name="options">options to specific implementations</param>
    /// <returns>Authentication object that is stored for the request. A user class or something like that.</returns>
    /// <exception cref="ArgumentException">if <paramref name="authenticationHeader"/> is invalid</exception>
    /// <exception cref="ArgumentNullException">If any of the parameters is empty or null.</exception>
    public abstract object Authenticate(
        string authenticationHeader, string realm, string httpVerb,
        params object[] options);

    /// <summary>
    /// Used to invoke the authentication delegate that is used to lookup the user name/realm.
    /// </summary>
    /// <param name="realm">Realm (domain) that user want to authenticate in</param>
    /// <param name="userName">User name</param>
    /// <param name="password">Password used for validation. Some implementations got password in clear text, they are then sent to client.</param>
    /// <param name="login">object that will be stored in the request to help you identify the user if authentication was successful.</param>
    /// <returns>true if authentication was successful</returns>
    protected bool CheckAuthentication(string realm, string userName, ref string password, out object login)
    {
      _authenticator(realm, userName, ref password, out login);
      return true;
    }

    /// <summary>
    /// Determines if authentication is required.
    /// </summary>
    /// <param name="request">HTTP request from browser</param>
    /// <returns>true if user should be authenticated.</returns>
    /// <remarks>throw <see cref="ForbiddenException"/> from your delegate if no more attempts are allowed.</remarks>
    /// <exception cref="ForbiddenException">If no more attempts are allowed</exception>
    public bool AuthenticationRequired(IHttpRequest request)
    {
      return _authenticationRequiredHandler != null && _authenticationRequiredHandler(request);
    }
  }
}