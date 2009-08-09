using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using HttpServer.Exceptions;
using HttpServer.Helpers;
using HttpServer.Rendering;
using HttpServer.Sessions;

namespace HttpServer.Controllers
{
    /// <summary>
    /// View controllers integrates the templates, by adding 
    /// Render methods.
    /// </summary>
    public abstract class ViewController : RequestController
    {
        private readonly TemplateArguments _arguments = new TemplateArguments();
        private readonly TemplateManager _templateMgr;
        private NameValueCollection _errors = new NameValueCollection();
        private bool _includeLayoutInAjaxRequests;
        private string _layout = "Application";
        private string _title;

        /// <summary>
        /// Create a new <see cref="ViewController"/>.
        /// </summary>
        protected ViewController(TemplateManager mgr)
        {
            _templateMgr = mgr;
        }

        /// <summary>
        /// Create a new <see cref="ViewController"/>.
        /// </summary>
        /// <param name="controller">prototype to copy information from.</param>
        protected ViewController(ViewController controller)
            : base(controller)
        {
            _templateMgr = controller._templateMgr;
        }

        /// <summary>
        /// Arguments that are being used in the templates.
        /// </summary>
        protected TemplateArguments Arguments
        {
            get { return _arguments; }
        }

        /// <summary>
        /// A set of errors that occurred during request processing.
        /// Key should be argument name (if argument error, otherwise <see cref="String.Empty"/>), value should be
        /// the error message.
        /// </summary>
        /// <remarks>Errors can be rendered into templates using the <see cref="WebHelper.Errors"/> method.</remarks>
        /// <seealso cref="WebHelper"/>
        [XmlElement("Errors")]
        protected NameValueCollection Errors
        {
            get { return _errors; }
            set { _errors = value; }
        }

        /// <summary>
        /// True if we always should render contents inside page layouts when request is Ajax.
        /// </summary>
        /// <remarks>default is false.</remarks>
        public bool IncludeLayoutInAjaxRequests
        {
            get { return _includeLayoutInAjaxRequests; }
            set { _includeLayoutInAjaxRequests = value; }
        }

        /// <summary>
        /// Which page layout to use (without file extension)
        /// </summary>
        /// <remarks>
        /// <para>
        /// Page layouts should be places in the Views\Layouts folder.
        /// </para>
        /// <para>
        /// default is "Application"
        /// </para>
        /// </remarks>
        public string Layout
        {
            get { return _layout; }
            set { _layout = value; }
        }

        /// <summary>
        /// Page title (are added as a parameter to the layout template, use it in &lt;title&gt; HTML tag.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        /// <summary>
        /// Render template for the currently invoked method.
        /// </summary>
        /// <param name="args">arguments/parameters used in template</param>
        /// <returns>template generated content</returns>
        /// <remarks>calls RenderActionWithErrors</remarks>
        protected string Render(params object[] args)
        {
            return RenderAction(MethodName, args);
        }

        /// <summary>
        /// Render contents into a template.
        /// </summary>
        /// <param name="method">method/template to generate</param>
        /// <param name="args">arguments/parameters used in template</param>
        /// <returns>template generated content</returns>
        /// <remarks>calls RenderActionWithErrors.</remarks>
        protected virtual string RenderAction(string method, params object[] args)
        {
            if (!Arguments.Contains("Errors"))
                Arguments.Add("Errors", _errors);
            string pageTemplate = RenderTemplate(ControllerName, method, args);

            // 1. dont render main layout for ajax requests, since they just update partial 
            // parts of the web page.
            // 2. Dont render html layout for other stuff than html
            if (Request.IsAjax && !_includeLayoutInAjaxRequests)
                return pageTemplate;

            return RenderLayout(_layout, pageTemplate);
        }

        /// <summary>
        /// Merge arguments array and Arguments property.
        /// </summary>
        /// <param name="args">Arguments array to merge</param>
        /// <returns>arguments/parameters that can be used in the template.</returns>
        /// <remarks>Will add Request/Response/Session arguments</remarks>
        private TemplateArguments MergeArguments(object[] args)
        {
            // Create a new argument holder
            TemplateArguments arguments = new TemplateArguments();
            arguments.Add("Request", Request, typeof(IHttpRequest));
            arguments.Add("Response", Response);
            arguments.Add("Session", Session);
            arguments.Add("Controller", this, typeof(ViewController));
			arguments.Update(_arguments);
			arguments.Update(new TemplateArguments(args));

            return arguments;
        }


        /// <summary>
        /// Renders errors from the <see cref="Errors"/> property into the
        /// current method template, or as a JavaScript alert if the request is Ajax.
        /// </summary>
        /// <param name="method">name of the currently invoked method.</param>
        /// <param name="arguments">arguments used in the method template.</param>
        /// <returns>generated string</returns>
        /// <remarks>Creates a JavaScript Alert box if request is Ajax.</remarks>
        protected string RenderErrors(string method, params object[] arguments)
        {
            if (_errors.Count > 0)
            {
                if (Request.IsAjax)
                    return RenderJsErrors(_errors);
            }

            return RenderAction(method, arguments);
        }

        /// <summary>
        /// Renders errors from the <see cref="Errors"/> property into the
        /// current method template, or as a JavaScript alert if the request is Ajax.
        /// </summary>
        /// <param name="errors">A collection of errors.</param>
        /// <param name="method">name of the currently invoked method.</param>
        /// <param name="arguments">arguments used in the method template.</param>
        /// <returns>generated string</returns>
        /// <remarks>Creates a JavaScript Alert box if request is Ajax.</remarks>
        protected string RenderErrors(NameValueCollection errors, string method, params object[] arguments)
        {
            if (_errors.Count > 0)
            {
                if (Request.IsAjax)
                    return RenderJsErrors(_errors);
            }

            Arguments.Add("Errors", errors);
            return RenderAction(method, arguments);
        }

        /// <summary>
        /// Switches content-type to "text/JavaScript" and returns content.
        /// </summary>
        /// <param name="js">JavaScript to send to the client.</param>
        /// <returns>JavaScript</returns>
        protected string RenderJavascript(string js)
        {
            Response.ContentType = "text/javascript";
            return js;
        }

        /// <summary>
        /// Creates a JavaScript "alert" filled with all errors.
        /// </summary>
        /// <param name="errors"></param>
        /// <returns>a</returns>
        protected string RenderJsErrors(NameValueCollection errors)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alert('");
            for (int i = 0; i < errors.Count; ++i)
                sb.Append(errors[i].Replace("'", "\\'") + "\\n");
            sb.Append("');");

            Response.ContentType = "text/javascript";
            return sb.ToString();
        }

        /// <summary>
        /// renders one of the layouts
        /// </summary>
        /// <param name="layoutName">layout to render (should be found in the "views\\layouts" folder).</param>
        /// <param name="contents">contents will be put in the template variable called "text".</param>
        /// <returns>generated text/HTML.</returns>
        protected virtual string RenderLayout(string layoutName, string contents)
        {
            // template engine will find all layouts and take the first one with that one.
            return RenderTemplate("layouts", layoutName, "text", contents,
                                  "title", Title ?? "The page with no name");
        }

        /// <summary>
        /// Render a template.
        /// </summary>
        /// <remarks>Merges the Arguments property with the <c>args</c> parameter and pass those to the template.</remarks>
        /// <param name="controller">controller name are used as a folder name when looking for the template.</param>
        /// <param name="method">method are used as filename when looking for the template.</param>
        /// <param name="args">arguments that should be passed to the template.</param>
        /// <returns></returns>
        protected string RenderTemplate(string controller, string method, params object[] args)
        {
            try
            {
                TemplateArguments args2 = MergeArguments(args);
                _arguments.Clear();

                string result = _templateMgr.Render(controller + "\\" + method + ".*", args2);
                _errors.Clear();
                return result;
            }
            catch (FileNotFoundException err)
            {
                throw new NotFoundException("Failed to find template. Details: " + err.Message, err);
            }
            catch (InvalidOperationException err)
            {
                throw new InternalServerException("Failed to render template. Details: " + err.Message, err);
            }
            catch (Fadd.CompilerException err)
            {
				throw new TemplateException("Could not compile template '" + controller + "/" + method + "/'", err);
            }
            catch (CodeGeneratorException err)
            {
#if DEBUG
                string error = "Line: " + err.LineNumber + "<br />\r\n" + err.ToString().Replace("\r\n", "<br />\r\n");
                throw new InternalServerException(error, err);
#else
                throw new InternalServerException("Failed to compile template.", err);
#endif
            }
            catch (ArgumentException err)
            {
#if DEBUG
                throw new InternalServerException(
                    "Failed to render template, reason: " + err.ToString().Replace("\r\n", "<br />\r\n"), err);
#else
                throw new InternalServerException("Failed to render templates", err);
#endif
            }
        }

        /// <summary>
        /// Invoked each time a new request is about to be invoked.
        /// </summary>
        /// <remarks>Can be used to clear old data.</remarks>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="session"></param>
        protected override void SetupRequest(IHttpRequest request, IHttpResponse response, IHttpSession session)
        {
            _arguments.Clear();
            _errors.Clear();
            base.SetupRequest(request, response, session);
            _layout = Request.Param["layout"].Value ?? Layout;
        }
    }
}