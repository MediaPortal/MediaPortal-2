using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using HttpServer.Authentication;
using HttpServer.Exceptions;
using HttpServer.HttpModules;
using HttpServer.Sessions;

namespace HttpServer.Controllers
{
    /// <summary>
    /// A controller in the Model-View-Controller pattern.
    ///  Derive this class and add method with one of the following signatures:
    /// "public string MethodName()" or "public void MyMethod()".
    /// 
    /// The first should return a string with the response, the latter
    /// should use SendHeader and SendBody methods to handle the response.
    /// </summary>
    /// <remarks>
    /// Last segment of the path is always broken into the properties Id and RequestedType
    /// Alhtough note that the RequestedType can also be empty if no file extension have
    /// been specified. A typical use of file extensions in controllers is to specify which type of
    /// format to return.
    /// </remarks>
    /// <example>
    /// public class MyController : RequestController
    /// {
    ///   public string Hello()
    ///   {
    ///       if (RequestedType == "xml")
    ///           return "&lt;hello&gt;World&lt;hello&gt;";
    ///       else
    ///           return "Hello " + Request.QueryString["user"].Value + ", welcome to my world";
    ///   }
    /// 
    ///   public void File()
    ///   {
    ///     Response.Headers.ContentType = "text/xml";
    ///     Response.SendHeader();
    ///   }
    /// }
    /// </example>
    /// <seealso cref="ControllerNameAttribute"/>
    /// <seealso cref="AuthRequiredAttribute"/>
    /// <seealso cref="AuthValidatorAttribute"/>
    public abstract class RequestController : HttpModule, ICloneable
    {
        private const string Html = "html";
        private readonly LinkedList<MethodInfo> _beforeFilters = new LinkedList<MethodInfo>();
        private readonly Dictionary<string, MethodInfo> _binaryMethods = new Dictionary<string, MethodInfo>();
        private readonly Dictionary<string, int> _authMethods = new Dictionary<string, int>();
        private readonly Dictionary<string, MethodInfo> _methods = new Dictionary<string, MethodInfo>();
        private LinkedListNode<MethodInfo> _lastMiddleFilter;
        private string _controllerName;
        private MethodInfo _defaultMethod;
        private string _defaultMethodStr;
        private MethodInfo _authValidator;

        //used temp during method mapping.
        private string _id;
        private MethodInfo _method;
        private string _methodName;
        private IHttpRequest _request;
        private string _requestedExtension;
        private IHttpResponse _response;
        private IHttpSession _session;

        /// <summary>
        /// create a new request controller
        /// </summary>
        /// <param name="controller">prototype to copy information from</param>
        protected RequestController(RequestController controller)
        {
            _beforeFilters = controller._beforeFilters;
            _binaryMethods = controller._binaryMethods;
            _authMethods = controller._authMethods;
            _methods = controller._methods;
            _controllerName = controller.ControllerName;
            _defaultMethod = controller._defaultMethod;
            _defaultMethodStr = controller._defaultMethodStr;
            _authValidator = controller._authValidator;
        }

        /// <summary>
        /// create a new controller
        /// </summary>
        protected RequestController()
        {
            MapMethods();
        }

        /// <summary>
        /// object that was attached during http authentication process.
        /// </summary>
        /// <remarks>
        /// You can also assign this tag yourself if you are using regular
        /// http page login.
        /// </remarks>
        /// <seealso cref="AuthModule"/>
        protected object AuthenticationTag
        {
            get { return _session[AuthModule.AuthenticationTag]; }
            set { _session[AuthModule.AuthenticationTag] = value; }
        }

        /// <summary>
        /// Name of this controller (class name without the "Controller" part)
        /// </summary>
        public string ControllerName
        {
            get { return _controllerName; }
        }

        /// <summary>
        /// Specifies the method to use if no action have been specified.
        /// </summary>
        /// <exception cref="ArgumentException">If specified method do not exist.</exception>
        public string DefaultMethod
        {
            get { return _defaultMethodStr; }
            set
            {
                if (_methods.ContainsKey(value.ToLower()))
                {
                    _defaultMethodStr = value.ToLower();
                    _defaultMethod = _methods[_defaultMethodStr];
                }
                else if (_binaryMethods.ContainsKey(value.ToLower()))
                {
                    _defaultMethodStr = value.ToLower();
                    _defaultMethod = _binaryMethods[_defaultMethodStr];
                }
                else
                    throw new ArgumentException("New DefaultMethod value is not a valid controller method.");
            }
        }

        /// <summary>
        /// Id is the third part of the uri path.
        /// </summary>
        /// <remarks>
        /// <para>Is extracted as in: /controllername/methodname/id/
        /// </para>
        /// <para>string.Empty if not specified.</para>
        /// </remarks>
        /// <example></example>
        public string Id
        {
            get { return _id ?? string.Empty; }
        }

        /// <summary>
        /// Method currently being invoked.
        /// Always in lower case.
        /// </summary>
        public string MethodName
        {
            get { return _methodName; }
        }

        /// <summary>
        /// Request information (like Url, form, querystring etc)
        /// </summary>
        protected IHttpRequest Request
        {
            get { return _request; }
        }

        /// <summary>
        /// Extension if a filename was specified.
        /// </summary>
        public string RequestedExtension
        {
            get { return _requestedExtension; }
        }

        /// <summary>
        /// Response information (that is going to be sent back to the browser/client)
        /// </summary>
        protected IHttpResponse Response
        {
            get { return _response; }
        }

        /// <summary>
        /// Session information, is stored between requests as long as the session cookie is valid.
        /// </summary>
        protected IHttpSession Session
        {
            get { return _session; }
        }

        /*
        protected virtual void AddAuthAttribute(string methodName, object attribute)
        {
            if (attribute.GetType() == typeof (AuthenticatorAttribute))
            {
                AuthenticatorAttribute attrib = (AuthenticatorAttribute) attribute;
                try
                {
                    MethodInfo mi = GetType().GetMethod(attrib.Method);
                    if (methodName == ClassMethodName)
                        _classCheckAuthMethod = mi;
                    else
                        _authMethods.Add(methodName, mi);
                }
                catch (AmbiguousMatchException err)
                {
                    if (methodName == "class")
                        throw new InvalidOperationException(
                            "Failed to find Authenticator method for class " + GetType().Name, err);
                    else
                        throw new InvalidOperationException("Failed to find Authenticator method for " + GetType().Name +
                                                            "." + methodName);
                }
            }
            else
                throw new ArgumentException("Attribute is not of type AuthenticatorAttribute");
        }
        */

        /// <summary>
        /// Method that determines if an url should be handled or not by the module
        /// </summary>
        /// <param name="request">Url requested by the client.</param>
        /// <returns>true if module should handle the url.</returns>
        public virtual bool CanHandle(IHttpRequest request)
        {
            if (request.UriParts.Length <= 0)
                return false;

            // check if controller name is correct. uri segments adds a slash to the segments
            if (string.Compare(request.UriParts[0], _controllerName, true) != 0)
                return false;

            // check action
            if (request.UriParts.Length > 1)
            {
                string uriPart = request.UriParts[1];
                int pos = uriPart.LastIndexOf('.');
                if (pos != -1)
                    uriPart = uriPart.Substring(0, pos);
                if (_methods.ContainsKey(uriPart) || _binaryMethods.ContainsKey(uriPart))
                    return true;
            }

            if (request.UriParts.Length == 1)
                return _defaultMethod != null;

            return false;
        }

        /// <summary>
        /// Determines which method to use.
        /// </summary>
        /// <param name="request">Requested resource</param>
        protected virtual MethodInfo GetMethod(IHttpRequest request)
        {
            // Check where the default met
            if (request.UriParts.Length <= 1)
                return _defaultMethod;

            string uriPart = request.UriParts[1];
            int pos = uriPart.LastIndexOf('.');
            if (pos != -1)
            {
                _requestedExtension = uriPart.Substring(pos + 1);
                uriPart = uriPart.Substring(0, pos);
            }

            if (_methods.ContainsKey(uriPart))
                return _methods[uriPart];
            
            if (_binaryMethods.ContainsKey(uriPart))
                return _binaryMethods[uriPart];


            return null;
        }

        /// <summary>
        /// Call all before filters
        /// </summary>
        /// <returns>true if a before filter wants to abort the processing.</returns>
        private bool InvokeBeforeFilters()
        {
            try
            {
                foreach (MethodInfo info in _beforeFilters)
                    if (!(bool) info.Invoke(this, null))
                        return true;

                return false;
            }
            catch (TargetInvocationException err)
            {
#if DEBUG
                FieldInfo remoteStackTraceString =
                    typeof(Exception).GetField("_remoteStackTraceString",
                                                BindingFlags.Instance | BindingFlags.NonPublic);
                remoteStackTraceString.SetValue(err.InnerException, err.InnerException.StackTrace + Environment.NewLine);
                throw err.InnerException;
#else
                    throw new InternalServerException("Controller filter failure, please try again.", err);
#endif
            }
        }

        /// <summary>
        /// Override this method to be able to process result
        /// returned by controller method.
        /// </summary>
        protected virtual void InvokeMethod()
        {
            try
            {
                InvokeMethodInternal();
            }
            catch (Exception err)
            {
                OnUnhandledException(err);
            }
        }

        /// <summary>
        /// Override this method if you want to be able to 
        /// handle unhanded exceptions
        /// </summary>
        /// <param name="err">thrown exception</param>
        /// <remarks>Don't "eat" exceptions derived from HttpException since
        /// they are handled by the framework,unless your are sure of what you are
        /// doing..</remarks>
        protected virtual void OnUnhandledException(Exception err)
        {
            throw err;
        }

        private void InvokeMethodInternal()
        {
            if (_authMethods.ContainsKey(_methodName))
            {
                if (_authValidator != null)
                {
                    if (_authValidator.GetParameters().Length == 1)
                    {
                        if (!(bool) _authValidator.Invoke(this, new object[] {_authMethods[_methodName]}))
                            return;
                    }
                    // backwards compatible.
                    else
                        if (!(bool)_authValidator.Invoke(this, null))
                            throw new UnauthorizedException("Need to authenticate.");
                }
            }

            if (_method.ReturnType == typeof (string))
            {
                try
                {
                    string temp = (string) _method.Invoke(this, null);
                    if (temp != null)
                    {
                        TextWriter writer = new StreamWriter(Response.Body);
                        writer.Write(temp);
                        writer.Flush();
                    }
                }
                catch (TargetInvocationException err)
                {
#if DEBUG
                    FieldInfo remoteStackTraceString =
                        typeof (Exception).GetField("_remoteStackTraceString",
                                                    BindingFlags.Instance | BindingFlags.NonPublic);
                    remoteStackTraceString.SetValue(err.InnerException, err.InnerException.StackTrace + Environment.NewLine);
                    throw err.InnerException;
#else
                    throw new InternalServerException("Controller failure, please try again.", err);
#endif
                }
            }
            else
            {
                _method.Invoke(this, null);
            }
        }
        /*
        /// <summary>
        /// check authentication attributes for the class
        /// </summary>
        protected virtual void MapClassAuth()
        {
            object[] attributes = GetType().GetCustomAttributes(true);
            foreach (object attribute in attributes)
            {
                if (attribute.GetType() == typeof (AuthenticatorAttribute))
                    AddAuthAttribute(ClassMethodName, attribute);
                if (attribute.GetType() == typeof (AuthenticationRequiredAttribute))
                    AddCheckAuthAttribute(ClassMethodName, attribute);
            }
        }
        */
        /// <summary>
        /// This method goes through all methods in the controller and
        /// add's them to a dictionary. They are later used to invoke
        /// the correct method depending on the url
        /// </summary>
        private void MapMethods()
        {
            lock (_methods)
            {
                // already mapped.
                if (_methods.Count > 0)
                    return;

                object[] controllerNameAttrs = GetType().GetCustomAttributes(typeof (ControllerNameAttribute), false);
                if (controllerNameAttrs.Length > 0)
                    _controllerName = ((ControllerNameAttribute)controllerNameAttrs[0]).Name;
                else
                {
                    _controllerName = GetType().Name;
                    if (ControllerName.Contains("Controller"))
                        _controllerName = ControllerName.Replace("Controller", "");
                    _controllerName = ControllerName.ToLower();
                }

                MethodInfo[] methods =
                    GetType().GetMethods(BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance);
                foreach (MethodInfo info in methods)
                {
                    ParameterInfo[] parameters = info.GetParameters();

                    // find regular render methods
                    if (parameters.Length == 0 && info.ReturnType == typeof (string))
                    {
                        string name = info.Name.ToLower();
                        if (name.Length > 3 && (name.Substring(0, 4) == "get_" || name.Substring(0, 4) == "set_"))
                            continue;
                        if (name == "tostring")
                            continue;

                        // Add authenticators
                        object[] authAttributes = info.GetCustomAttributes(true);
                        foreach (object attribute in authAttributes)
                            if (attribute.GetType() == typeof (AuthRequiredAttribute))
                                _authMethods.Add(info.Name.ToLower(), ((AuthRequiredAttribute)attribute).Level);
                        _methods.Add(info.Name.ToLower(), info);
                    }

                    // find raw handlers 
                    object[] attributes = info.GetCustomAttributes(typeof (RawHandlerAttribute), true);
                    if (attributes.Length >= 1 && info.ReturnType == typeof (void) && parameters.Length == 0)
                    {
                        // Add authenticators
                        object[] authAttributes = info.GetCustomAttributes(true);
                        foreach (object attribute in authAttributes)
                            if (attribute.GetType() == typeof(AuthRequiredAttribute))
                                _authMethods.Add(info.Name.ToLower(), ((AuthRequiredAttribute)attribute).Level);
                        _binaryMethods.Add(info.Name.ToLower(), info);
                    }
                } //foreach

                methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (MethodInfo info in methods)
                {
                    ParameterInfo[] parameters = info.GetParameters();

                    // find before filters.
                    if (parameters.Length == 0 && info.ReturnType == typeof (bool))
                    {
                        object[] authAttributes = info.GetCustomAttributes(true);
                        foreach (object attribute in authAttributes)
                            if (attribute.GetType() == typeof(AuthValidatorAttribute))
                            {
                                if (_authValidator != null)
                                    throw new InvalidOperationException("Auth validator have already been specified.");
                                _authValidator = info;
                            }
                            else if (attribute.GetType() == typeof (BeforeFilterAttribute))
                            {
                                BeforeFilterAttribute attr = (BeforeFilterAttribute) attribute;
                                LinkedListNode<MethodInfo> node = new LinkedListNode<MethodInfo>(info);


                                if (attr.Position == FilterPosition.First)
                                    _beforeFilters.AddFirst(node);
                                else if (attr.Position == FilterPosition.Last)
                                    _beforeFilters.AddLast(node);
                                else
                                {
                                    if (_lastMiddleFilter == null)
                                        _beforeFilters.AddLast(node);
                                    else
                                        _beforeFilters.AddAfter(_lastMiddleFilter, node);

                                    _lastMiddleFilter = node;
                                }
                            }
                    }
                }

                // Map index method.
                MethodInfo mi = GetType().GetMethod("Index", BindingFlags.Public | BindingFlags.Instance);
                if (mi != null && mi.ReturnType == typeof(string) && mi.GetParameters().Length == 0)
                    DefaultMethod = "Index";
            }
        }

        /// <summary>
        /// Method that process the url
        /// </summary>
        /// <param name="request">Uses Uri and QueryString to determine method.</param>
        /// <param name="response">Relays response object to invoked method.</param>
        /// <param name="session">Relays session object to invoked method. </param>
        public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
        {
            if (!CanHandle(request))
                return false;

            SetupRequest(request, response, session);

            if (InvokeBeforeFilters())
                return true;

            InvokeMethod();

            return true;
        }

        /// <summary>
        /// Will assign all variables that are unique for each session
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="session"></param>
        protected virtual void SetupRequest(IHttpRequest request, IHttpResponse response, IHttpSession session)
        {
            _requestedExtension = Html;

            // extract id
            if (request.Uri.Segments.Length > 3)
            {
                _id = request.Uri.Segments[3];
                if (_id.EndsWith("/"))
                    _id = _id.Substring(0, _id.Length - 1);
                else
                {
                    int pos = _id.LastIndexOf('.');
                    if (pos != -1)
                    {
                        _requestedExtension = _id.Substring(pos + 1);
                        _id = _id.Substring(0, pos);
                    }
                }
                _id = HttpUtility.UrlDecode(_id);
            }
            else if (request.QueryString["id"] != HttpInputItem.Empty)
                _id = HttpUtility.UrlDecode(request.QueryString["id"].Value);
            else
                _id = string.Empty;

            _request = request;
            _response = response;
            _session = session;

            if (request.Uri.Segments.Length == 2 && _defaultMethod == null)
                throw new NotFoundException("No default method is specified.");

            _method = GetMethod(request);
            if (_method == null)
                throw new NotFoundException("Requested action could not be found.");

            _methodName = _method.Name.ToLower();
        }

        #region ICloneable Members

        /// <summary>
        /// Make a clone of this controller
        /// </summary>
        /// <returns>a new controller with the same base information as this one.</returns>
        public abstract object Clone();

        #endregion
    }
}