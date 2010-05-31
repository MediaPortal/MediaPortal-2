using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using HttpServer.Authentication;
using HttpServer.Exceptions;
using HttpServer.FormDecoders;
using HttpServer.HttpModules;
using HttpServer.Rules;
using HttpServer.Sessions;

namespace HttpServer
{
  /// <summary>
  /// Delegate used to find a realm/domain.
  /// </summary>
  /// <param name="domain"></param>
  /// <returns></returns>
  /// <remarks>
  /// Realms are used during HTTP Authentication
  /// </remarks>
  /// <seealso cref="AuthenticationModule"/>
  /// <seealso cref="AuthenticationHandler"/>
  public delegate string RealmHandler(string domain);

  /// <summary>
  /// A complete HTTP server, you need to add a module to it to be able to handle incoming requests.
  /// </summary>
  /// <example>
  /// <code>
  /// // This small example will add two web site modules, thus handling
  /// // two different sites. In reality you should add Controller modules or something
  /// // two the website modules to be able to handle different requests.
  /// HttpServer server = new HttpServer();
  /// server.Add(new WebSiteModule("www.gauffin.com", "Gauffin Telecom AB"));
  /// server.Add(new WebSiteModule("www.vapadi.se", "Remote PBX"));
  /// 
  /// // start regular http
  /// server.Start(IPAddress.Any, 80);
  /// 
  /// // start https
  /// server.Start(IPAddress.Any, 443, myCertificate);
  /// </code>
  /// </example>
  /// <seealso cref="HttpModule"/>
  /// <seealso cref="FileModule"/>
  /// <seealso cref="HttpListener"/>
  public class HttpServer
  {
    private FormDecoderProvider _formDecodersProvider;
    private readonly List<HttpModule> _modules = new List<HttpModule>();
    private readonly List<IRule> _rules = new List<IRule>();
    private readonly List<AuthenticationModule> _authModules = new List<AuthenticationModule>();
    private HttpListener _httpListener;
    private HttpListener _httpsListener;
    private string _serverName = "SeeSharpWebServer";
    private string _sessionCookieName = "__tiny_sessid";
    private IHttpSessionStore _sessionStore;
    private ILogWriter _logWriter = NullLogWriter.Instance;
    private int _backLog = 10;
    private ExceptionHandler _exceptionHandler;
    private readonly IComponentProvider _components;
    private RequestQueue _requestQueue;
    [ThreadStatic] private static HttpServer _current;

    /// <summary>
    /// Server that is handling the current request.
    /// </summary>
    /// <remarks>
    /// Will be set as soon as a request arrives to the <see cref="HttpServer"/> object.
    /// </remarks>
    public static HttpServer Current
    {
      get { return _current; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class.
    /// </summary>
    /// <param name="provider">Used to get all components used in the server..</param>
    public HttpServer(IComponentProvider provider)
    {
      _components = provider;
      _requestQueue = new RequestQueue(ProcessRequestWrapper);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class.
    /// </summary>
    public HttpServer()
    {
      _components = new ComponentProvider();
      _requestQueue = new RequestQueue(ProcessRequestWrapper);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class.
    /// </summary>
    /// <param name="decoderProvider">Form decoders are used to convert different types of posted data to the <see cref="HttpInput"/> object types.</param>
    /// <seealso cref="IFormDecoder"/>
    /// <seealso cref="FormDecoderProviders"/>
    public HttpServer(FormDecoderProvider decoderProvider)
    {
      Check.Require(decoderProvider, "decoderProvider");
      _components = new ComponentProvider();
      _components.AddInstance<FormDecoderProvider>(decoderProvider);
      _requestQueue = new RequestQueue(ProcessRequestWrapper);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class.
    /// </summary>
    /// <param name="sessionStore">A session store is used to save and retrieve sessions</param>
    /// <seealso cref="IHttpSessionStore"/>
    public HttpServer(IHttpSessionStore sessionStore)
    {
      Check.Require(sessionStore, "sessionStore");
      _components = new ComponentProvider();
      _components.AddInstance<IHttpSessionStore>(sessionStore);
      _requestQueue = new RequestQueue(ProcessRequestWrapper);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class.
    /// </summary>
    /// <param name="logWriter">The log writer.</param>
    /// <seealso cref="LogWriter"/>
    public HttpServer(ILogWriter logWriter)
    {
      Check.Require(logWriter, "logWriter");
      _components = new ComponentProvider();
      _components.AddInstance<ILogWriter>(logWriter);
      _requestQueue = new RequestQueue(ProcessRequestWrapper);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class.
    /// </summary>
    /// <param name="decoderProvider">Form decoders are used to convert different types of posted data to the <see cref="HttpInput"/> object types.</param>
    /// <param name="logWriter">The log writer.</param>
    /// <seealso cref="IFormDecoder"/>
    /// <seealso cref="FormDecoderProviders"/>
    /// <seealso cref="LogWriter"/>
    public HttpServer(FormDecoderProvider decoderProvider, ILogWriter logWriter)
    {
      Check.Require(decoderProvider, "decoderProvider");
      Check.Require(logWriter, "logWriter");
      _components = new ComponentProvider();
      _components.AddInstance<FormDecoderProvider>(decoderProvider);
      _requestQueue = new RequestQueue(ProcessRequestWrapper);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServer"/> class.
    /// </summary>
    /// <param name="decoderProvider">Form decoders are used to convert different types of posted data to the <see cref="HttpInput"/> object types.</param>
    /// <param name="sessionStore">A session store is used to save and retrieve sessions</param>
    /// <param name="logWriter">The log writer.</param>
    /// <seealso cref="IFormDecoder"/>
    /// <seealso cref="FormDecoderProviders"/>
    /// <seealso cref="LogWriter"/>
    /// <seealso cref="IHttpSessionStore"/>
    public HttpServer(FormDecoderProvider decoderProvider, IHttpSessionStore sessionStore, ILogWriter logWriter)
    {
      Check.Require(decoderProvider, "decoderProvider");
      Check.Require(sessionStore, "sessionStore");
      _components = new ComponentProvider();
      _components.AddInstance<FormDecoderProvider>(sessionStore);
      _components.AddInstance<IHttpSessionStore>(sessionStore);
      if (logWriter != null)
        _components.AddInstance<ILogWriter>(logWriter);
      _requestQueue = new RequestQueue(ProcessRequestWrapper);
    }

    /// <summary>
    /// Modules used for authentication. The module that is is added first is used as 
    /// the default authentication module.
    /// </summary>
    /// <remarks>Use the corresponding property
    /// in the <see cref="WebSiteModule"/> if you are using multiple websites.</remarks>
    public IList<AuthenticationModule> AuthenticationModules
    {
      get { return _authModules; }
    }

    /// <summary>
    /// Form decoder providers are used to decode request body (which normally contains form data).
    /// </summary>
    public FormDecoderProvider FormDecoderProviders
    {
      get
      {
        if (_formDecodersProvider == null)
          _formDecodersProvider = _components.Get<FormDecoderProvider>() ?? new FormDecoderProvider();
        return _formDecodersProvider;
      }
    }

    /// <summary>
    /// Server name sent in HTTP responses.
    /// </summary>
    /// <remarks>
    /// Do NOT include version in name, since it makes it 
    /// easier for hackers.
    /// </remarks>
    public string ServerName
    {
      get { return _serverName; }
      set { _serverName = value; }
    }

    /// <summary>
    /// Name of cookie where session id is stored.
    /// </summary>
    public string SessionCookieName
    {
      get { return _sessionCookieName; }
      set { _sessionCookieName = value; }
    }

    /// <summary>
    /// Specified where logging should go.
    /// </summary>
    /// <seealso cref="NullLogWriter"/>
    /// <seealso cref="ConsoleLogWriter"/>
    /// <seealso cref="LogWriter"/>
    public ILogWriter LogWriter
    {
      get { return _logWriter; }
      set
      {
        _logWriter = value ?? NullLogWriter.Instance;
        foreach (HttpModule module in _modules)
          module.SetLogWriter(_logWriter);
      }
    }

    /// <summary>
    /// Number of connections that can wait to be accepted by the server.
    /// </summary>
    /// <remarks>Default is 10.</remarks>
    public int BackLog
    {
      get { return _backLog; }
      set { _backLog = value; }
    }

    /// <summary>
    /// Gets or sets maximum number of allowed simultaneous requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is useful in busy systems. The HTTP server
    /// will start queuing new requests if this limit is hit, instead
    /// of trying to process all incoming requests directly.
    /// </para>
    /// <para>
    /// The default number if allowed simultaneous requests are 10.
    /// </para>
    /// </remarks>
    public int MaxRequestCount
    {
      get { return _requestQueue.MaxRequestCount; }
      set { _requestQueue.MaxRequestCount = value; }
    }

    /// <summary>
    /// Gets or sets maximum number of requests queuing to be handled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The WebServer will start turning requests away if response code
    /// <see cref="HttpStatusCode.ServiceUnavailable"/> to indicate that the server
    /// is too busy to be able to handle the request.
    /// </para>
    /// </remarks>
    public int MaxQueueSize
    {
      get { return _requestQueue.MaxQueueSize; }
      set { _requestQueue.MaxQueueSize = value; }
    }


    /// <summary>
    /// Adds the specified rule.
    /// </summary>
    /// <param name="rule">The rule.</param>
    public void Add(IRule rule)
    {
      _rules.Add(rule);
    }

    /// <summary>
    /// Add a <see cref="HttpModule"/> to the server.
    /// </summary>
    /// <param name="module">Module to add.</param>
    public void Add(HttpModule module)
    {
      _modules.Add(module);
    }

    /// <summary>
    /// Removes a <see cref="HttpModule"/> from the server.
    /// </summary>
    /// <param name="module">Module to remove.</param>
    // Added by Albert, Team-MediaPortal: It must be possible to remove modules at runtime.
    public void Remove(HttpModule module)
    {
      _modules.Remove(module);
    }

    /// <summary>
    /// Decodes the request body.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <exception cref="InternalServerException">Failed to decode form data.</exception>
    protected virtual void DecodeBody(IHttpRequest request)
    {
      try
      {
        if (request.Body.Length > 0)
          request.DecodeBody(_formDecodersProvider);
      }
      catch (InvalidOperationException err)
      {
        throw new InternalServerException("Failed to decode form data.", err);
      }
      catch (InvalidDataException err)
      {
        throw new InternalServerException("Form contains invalid format.", err);
      }
    }

    /// <summary>
    /// Generate a HTTP error page (that will be added to the response body).
    /// response status code is also set.
    /// </summary>
    /// <param name="response">Response that the page will be generated in.</param>
    /// <param name="error"><see cref="HttpStatusCode"/>.</param>
    /// <param name="body">response body contents.</param>
    protected virtual void ErrorPage(IHttpResponse response, HttpStatusCode error, string body)
    {
      response.Reason = "Internal server error";
      response.Status = error;
      response.ContentType = "text/plain";

      StreamWriter writer = new StreamWriter(response.Body);
      writer.WriteLine(body);
      writer.Flush();
    }

    /// <summary>
    /// Generate a HTTP error page (that will be added to the response body).
    /// response status code is also set.
    /// </summary>
    /// <param name="response">Response that the page will be generated in.</param>
    /// <param name="err">exception.</param>
    protected virtual void ErrorPage(IHttpResponse response, HttpException err)
    {
      response.Reason = err.GetType().Name;
      response.Status = err.HttpStatusCode;
      response.ContentType = "text/plain";
      StreamWriter writer = new StreamWriter(response.Body);
#if DEBUG
      writer.WriteLine(err);
#else
			writer.WriteLine(err.Message);
#endif
      writer.Flush();
    }

    /// <summary>
    /// Realms are used by the <see cref="AuthenticationModule"/>s.
    /// </summary>
    /// <param name="request">HTTP request</param>
    /// <returns>domain/realm.</returns>
    protected virtual string GetRealm(IHttpRequest request)
    {
      return RealmWanted != null ? RealmWanted(request.Headers["host"] ?? "localhost") : ServerName;
    }

    /// <summary>
    /// Process an incoming request.
    /// </summary>
    /// <param name="context">connection to client</param>
    /// <param name="request">request information</param>
    /// <param name="response">response that should be filled</param>
    /// <param name="session">session information</param>
    protected virtual void HandleRequest(
        IHttpClientContext context, IHttpRequest request, IHttpResponse response,
        IHttpSession session)
    {
      _logWriter.Write(this, LogPrio.Trace, "Processing request....");
      bool handled = false;
      try
      {
        DecodeBody(request);
        if (ProcessAuthentication(request, response, session))
        {
          foreach (HttpModule module in _modules)
          {
            if (!module.Process(request, response, session))
              continue;

            handled = true;
            if (!module.AllowSecondaryProcessing)
              break;
          }
        }
      }
      catch (HttpException err)
      {
        if (err.HttpStatusCode == HttpStatusCode.Unauthorized)
        {
          AuthenticationModule mod;
          lock (_authModules)
            mod = _authModules.Count > 0 ? _authModules[0] : null;

          if (mod != null)
            RequestAuthentication(mod, request, response);
        }
        else
          ErrorPage(response, err);
      }

      if (!handled && response.Status == HttpStatusCode.OK)
        ErrorPage(response, HttpStatusCode.NotFound, "Resource not found: " + request.Uri);

      if (!response.HeadersSent)
      {
        // Dispose session if it was not used.
        if (session.Count > 0)
        {
          _sessionStore.Save(session);
          // only set session cookie if it have not been sent in the request.
          if (request.Cookies[_sessionCookieName] == null)
            response.Cookies.Add(new ResponseCookie(_sessionCookieName, session.Id, DateTime.MinValue));
                //DateTime.Now.AddMinutes(20).AddDays(1)));
        }
        else
          _sessionStore.AddUnused(session);
      }

      if (!response.Sent)
        response.Send();

      _logWriter.Write(this, LogPrio.Trace, "....done.");
    }

    private void Init()
    {
      _sessionStore = _components.Get<IHttpSessionStore>();
      if (_sessionStore == null)
      {
        WriteLog(this, LogPrio.Info, "Defaulting to memory session store.");
        _sessionStore = new MemorySessionStore();
      }

      // add default decoders if none have been added.
      if (FormDecoderProviders.Count == 0)
      {
        WriteLog(
            this, LogPrio.Info,
            "Loading UrlDecoder, XmlDecoder and MultipartDecoder, since no decoders have been added.");
        _formDecodersProvider.Add(new UrlDecoder());
        _formDecodersProvider.Add(new MultipartDecoder());
        _formDecodersProvider.Add(new XmlDecoder());
      }

      // Components for building requests.
      if (!_components.Contains(typeof(IRequestParserFactory)))
        _components.Add<IRequestParserFactory, RequestParserFactory>();
      if (!_components.Contains(typeof(IHttpContextFactory)))
        _components.AddInstance<IHttpContextFactory>(
            new HttpContextFactory(
                LogWriter, 16384,
                _components.Get
                    <IRequestParserFactory>()));

      // the special folder does not exist on mono
      string tempPath = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
      if (string.IsNullOrEmpty(tempPath))
        tempPath = "/var/tmp/";
      if (!Directory.Exists(tempPath))
      {
        WriteLog(this, LogPrio.Warning, "Temp path do not exist: " + tempPath);
        return;
      }
      DirectoryInfo info = new DirectoryInfo(tempPath);
      foreach (FileInfo file in info.GetFiles("*.tmp"))
        file.Delete();

      _requestQueue.Start();
    }


    /// <summary>
    /// Can be overloaded to implement stuff when a client have been connected.
    /// </summary>
    /// <remarks>
    /// Default implementation does nothing.
    /// </remarks>
    /// <param name="client">client that disconnected</param>
    /// <param name="error">disconnect reason</param>
    protected virtual void OnClientDisconnected(IHttpClientContext client, SocketError error)
    {
    }

    /// <summary>
    /// Handle authentication
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <param name="session"></param>
    /// <returns>true if request can be handled; false if not.</returns>
    /// <exception cref="BadRequestException">Invalid authorization header</exception>
    protected virtual bool ProcessAuthentication(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      if (_authModules.Count > 0)
      {
        bool authenticate = false;
        object authTag = null;
        if (request.Headers["authorization"] != null)
        {
          authenticate = true;
          string authHeader = request.Headers["authorization"];
          int pos = authHeader.IndexOf(' ');
          if (pos == -1)
            throw new BadRequestException("Invalid authorization header");
          // first word identifies the type of authentication to use.
          string word = authHeader.Substring(0, pos).ToLower();

          // find the mod to use.
          AuthenticationModule mod = null;
          lock (_authModules)
          {
            foreach (AuthenticationModule aModule in _authModules)
            {
              if (aModule.Name != word)
                continue;
              mod = aModule;
              break;
            }
          }
          if (mod != null)
          {
            authTag = mod.Authenticate(authHeader, GetRealm(request), request.Method);
            session[AuthenticationModule.AuthenticationTag] = authTag;
          }
        }


        // Check if auth is needed.
        if (authTag == null)
        {
          lock (_authModules)
          {
            foreach (AuthenticationModule module in _authModules)
            {
              if (!module.AuthenticationRequired(request))
                continue;

              RequestAuthentication(module, request, response);
              return false;
            }

            // modules can have inited the authentication
            // and then the module.AuthenticationRequired method will not have been used.
            if (authenticate && _authModules.Count > 0)
            {
              RequestAuthentication(_authModules[0], request, response);
              return false;
            }
          }
        }
      }

      return true;
    }

    /// <summary>
    /// Will request authentication.
    /// </summary>
    /// <remarks>
    /// Sends respond to client, nothing else can be done with the response after this.
    /// </remarks>
    /// <param name="mod"></param>
    /// <param name="request"></param>
    /// <param name="response"></param>
    protected virtual void RequestAuthentication(AuthenticationModule mod, IHttpRequest request, IHttpResponse response)
    {
      string theResponse = mod.CreateResponse(GetRealm(request));
      response.AddHeader("www-authenticate", theResponse);
      response.Reason = "Authentication required.";
      response.Status = HttpStatusCode.Unauthorized;
    }

    /// <summary>
    /// Received from a <see cref="IHttpClientContext"/> when a request have been parsed successfully.
    /// </summary>
    /// <param name="source"><see cref="IHttpClientContext"/> that received the request.</param>
    /// <param name="args">The request.</param>
    private void OnRequest(object source, RequestEventArgs args)
    {
      _current = this;
      IHttpClientContext context = (IHttpClientContext) source;
      IHttpRequest request = args.Request;
      if (_requestQueue.ShouldQueue)
      {
        _requestQueue.Enqueue(context, request);
        return;
      }

      ProcessRequestWrapper(context, request);

      // no need to lock, if all threads are busy,
      // someone is bound to trigger the thread correctly =)
      _requestQueue.Trigger();
    }

    /// <summary>
    /// To be able to track request count.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="request"></param>
    private void ProcessRequestWrapper(IHttpClientContext context, IHttpRequest request)
    {
      _requestQueue.CurrentRequestCount += 1;
      ProcessRequest(context, request);
      _requestQueue.CurrentRequestCount -= 1;
    }

    private void ProcessRequest(IHttpClientContext context, IHttpRequest request)
    {
      IHttpResponse response = request.CreateResponse(context);
      try
      {
        foreach (IRule rule in _rules)
        {
          if (!rule.Process(request, response))
            continue;
          response.Send();
          return;
        }

        // load cookies if the exist.
        RequestCookies cookies = request.Headers["cookie"] != null
            ? new RequestCookies(request.Headers["cookie"])
            : new RequestCookies(string.Empty);

        request.SetCookies(cookies);

        IHttpSession session;
        if (cookies[_sessionCookieName] != null)
        {
          string sessionCookie = cookies[_sessionCookieName].Value;

          // there's a bug somewhere which fucks up headers which can render the session cookie useless.
          // therefore let's consider the session cookie as not set if that have happened.
          if (sessionCookie.Length > 40)
          {
            _logWriter.Write(this, LogPrio.Error, "Session cookie is invalid: " + sessionCookie);
            cookies.Remove(_sessionCookieName);
            _sessionStore.Remove(sessionCookie); // free the session cookie (and thus generating a new one).
            session = _sessionStore.Create();
          }
          else
            session = _sessionStore.Load(sessionCookie) ??
                _sessionStore.Create(sessionCookie);
        }
        else
          session = _sessionStore.Create();

        HandleRequest(context, request, response, session);
      }
      catch (Exception err)
      {
        if (_exceptionHandler == null)
#if DEBUG
          throw;
#else
				{
					WriteLog(LogPrio.Fatal, err.Message);
					return;
				}
#endif
        _exceptionHandler(this, err);

        Exception e = err;
        while (e != null)
        {
          if (e is SocketException)
            return;

          e = e.InnerException;
        }

        try
        {
#if DEBUG
          context.Respond(
              "HTTP/1.0", HttpStatusCode.InternalServerError, "Internal server error", err.ToString(), "text/plain");
#else
					context.Respond("HTTP/1.0", HttpStatusCode.InternalServerError, "Internal server error");
#endif
        }
        catch (Exception err2)
        {
          LogWriter.Write(this, LogPrio.Fatal, "Failed to respond on message with Internal Server Error: " + err2);
        }
      }
    }

    /// <summary>
    /// Start the web server using regular HTTP.
    /// </summary>
    /// <param name="address">IP Address to listen on, use <c>IpAddress.Any </c>to accept connections on all IP addresses/network cards.</param>
    /// <param name="port">Port to listen on. 80 can be a good idea =)</param>
    /// <exception cref="ArgumentNullException"><c>address</c> is null.</exception>
    /// <exception cref="ArgumentException">Port must be a positive number.</exception>
    public void Start(IPAddress address, int port)
    {
      if (address == null)
        throw new ArgumentNullException("address");
      if (port <= 0)
        throw new ArgumentException("Port must be a positive number.");
      if (_httpListener != null)
        return;


      Init();
      _httpListener = new HttpListener(address, port, _components.Get<IHttpContextFactory>()) {LogWriter = LogWriter};
      _httpListener.RequestReceived += OnRequest;
      _httpListener.Start(50);
      _httpListener.ExceptionThrown += _exceptionHandler;
    }

    /// <summary>
    /// Accept secure connections.
    /// </summary>
    /// <param name="address">IP Address to listen on, use <see cref="IPAddress.Any"/> to accept connections on all IP Addresses / network cards.</param>
    /// <param name="port">Port to listen on. 80 can be a good idea =)</param>
    /// <param name="certificate">Certificate to use</param>
    /// <exception cref="ArgumentNullException"><c>address</c> is null.</exception>
    /// <exception cref="ArgumentException">Port must be a positive number.</exception>
    public void Start(IPAddress address, int port, X509Certificate certificate)
    {
      if (address == null)
        throw new ArgumentNullException("address");
      if (port <= 0)
        throw new ArgumentException("Port must be a positive number.");
      if (_httpsListener != null)
        return;

      Init();
      _httpsListener = new HttpListener(address, port, _components.Get<IHttpContextFactory>(), certificate);
      _httpsListener.LogWriter = LogWriter;
      _httpsListener.RequestReceived += OnRequest;
      _httpsListener.Start(5);
      _httpsListener.ExceptionThrown += _exceptionHandler;
    }

    /// <summary>
    /// shut down the server and listeners
    /// </summary>
    public void Stop()
    {
      if (_httpListener != null)
      {
        _httpListener.Stop();
        _httpListener = null;
      }
      if (_httpsListener != null)
      {
        _httpsListener.Stop();
        _httpsListener = null;
      }

      _requestQueue.Stop();
    }

    /// <summary>
    /// write an entry to the log file
    /// </summary>
    /// <param name="prio">importance of the message</param>
    /// <param name="message">log message</param>
    protected virtual void WriteLog(LogPrio prio, string message)
    {
      LogWriter.Write(this, prio, message);
    }

    /// <summary>
    /// write an entry to the log file
    /// </summary>
    /// <param name="source">object that wrote the message</param>
    /// <param name="prio">importance of the message</param>
    /// <param name="message">log message</param>
    public void WriteLog(object source, LogPrio prio, string message)
    {
      LogWriter.Write(source, prio, message);
    }

    /// <summary>
    /// Realms are used during HTTP authentication.
    /// Default realm is same as server name.
    /// </summary>
    public event RealmHandler RealmWanted;

    /// <summary>
    /// Let's to receive unhandled exceptions from the threads.
    /// </summary>
    /// <remarks>
    /// Exceptions will be thrown during debug mode if this event is not used,
    /// exceptions will be printed to console and suppressed during release mode.
    /// </remarks>
    public event ExceptionHandler ExceptionThrown
    {
      add
      {
        _exceptionHandler += value;
        if (_httpListener != null)
          _httpListener.ExceptionThrown += value;
        if (_httpsListener != null)
          _httpsListener.ExceptionThrown += value;
      }
      remove
      {
        _exceptionHandler -= value;
        if (_httpListener != null)
          _httpListener.ExceptionThrown -= value;
        if (_httpsListener != null)
          _httpsListener.ExceptionThrown -= value;
      }
    }
  }
}