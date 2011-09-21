using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using Xunit;

namespace HttpServer.Controllers
{
    /// <summary>
    /// Used to simply testing of controls.
    /// </summary>
    public class ControllerTester
    {
        /// <summary>
        /// Fake host name, default is "http://localhost"
        /// </summary>
        public string HostName = "http://localhost";

        private static readonly IHttpClientContext TestContext = new MyContext();

        /// <summary>
        /// Session used if null have been specified as argument to one of the class methods.
        /// </summary>
        public IHttpSession DefaultSession = new MemorySession("abc");

        /// <summary>
        /// Send a GET request to a controller.
        /// </summary>
        /// <param name="controller">Controller receiving the post request.</param>
        /// <param name="uri">Uri visited.</param>
        /// <param name="response">Response from the controller.</param>
        /// <param name="session">Session used during the test. null = <see cref="DefaultSession"/> is used.</param>
        /// <returns>body posted by the response object</returns>
        /// <example>
        /// <code>
        /// void MyTest()
        /// {
        ///     ControllerTester tester = new ControllerTester();
        ///     
        ///     MyController controller = new MyController();
        ///     IHttpResponse response;
        ///     string text = Get(controller, "/my/hello/1?hello=world", out response, null);
        ///     Assert.Equal("world|1", text);
        /// }
        /// </code>
        /// </example>
        public string Get(RequestController controller, string uri, out IHttpResponse response, IHttpSession session)
        {
            return Invoke(controller, Method.Get, uri, out response, session);
        }

        /// <summary>
        /// Send a POST request to a controller.
        /// </summary>
        /// <param name="controller">Controller receiving the post request.</param>
        /// <param name="uri">Uri visited.</param>
        /// <param name="form">Form being processed by controller.</param>
        /// <param name="response">Response from the controller.</param>
        /// <param name="session">Session used during the test. null = <see cref="DefaultSession"/> is used.</param>
        /// <returns>body posted by the response object</returns>
        /// <example>
        /// <code>
        /// void MyTest()
        /// {
        ///     // Create a controller.
        ///     MyController controller = new MyController();
        ///
        ///     // build up a form that is used by the controller.
        ///     HttpForm form = new HttpForm();
        ///     form.Add("user[firstName]", "Jonas");
        /// 
        ///     // Invoke the request
        ///     ControllerTester tester = new ControllerTester();
        ///     IHttpResponse response;
        ///     string text = tester.Get(controller, "/user/create/", form, out response, null);
        /// 
        ///     // validate response back from controller.
        ///     Assert.Equal("User 'Jonas' has been created.", text);
        /// }
        /// </code>
        /// </example>
        public string Post(RequestController controller, string uri, HttpForm form, out IHttpResponse response, IHttpSession session)
        {
            return Invoke(controller, Method.Post, uri, form, out response, session);
        }


        private string Invoke(RequestController controller, string httpMetod, string uri, out IHttpResponse response, IHttpSession session)
        {
            return Invoke(controller, httpMetod, uri, null, out response, session);
        }

// ReSharper disable SuggestBaseTypeForParameter
        private string Invoke(RequestController controller, string httpMetod, string uri, HttpForm form, out IHttpResponse response, IHttpSession session)
// ReSharper restore SuggestBaseTypeForParameter
        {
            HttpRequest request = new HttpRequest
                                  	{
                                  		HttpVersion = "HTTP/1.1",
                                  		UriPath = uri,
                                  		Method = httpMetod,
                                  		Uri = new Uri(HostName + uri)
                                  	};
        	request.AssignForm(form);

        	response = request.CreateResponse(TestContext);
            if(!controller.Process(request, response, session))
                throw new NotFoundException("404 could not find processor for: " + uri);
           
            response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(response.Body);
            return reader.ReadToEnd();
        }

        [Fact]
        private void TestGet()
        {
            MyController controller = new MyController();
            IHttpResponse response;
            IHttpSession session = DefaultSession;
            string text = Get(controller, "/my/hello/1?hello=world", out response, session);
            Assert.Equal("world|1", text);
        }

        [Fact]
        private void TestPost()
        {
            MyController controller = new MyController();
            IHttpResponse response;
            IHttpSession session = DefaultSession;
            HttpForm form = new HttpForm();
            form.Add("user[firstName]", "jonas");
            string text = Post(controller, "/my/hello/id", form, out response, session);
            Assert.Equal("jonas", text);
        }


        private class MyController : RequestController
        {
            //controller method.
// ReSharper disable UnusedMemberInPrivateClass
            public string Hello()
// ReSharper restore UnusedMemberInPrivateClass
            {
                Assert.False(string.IsNullOrEmpty(Id));
                if (Request.Method == Method.Post)
                    return Request.Form["user"]["firstName"].Value;

                return Request.QueryString["hello"].Value + "|" + Id; 
            }

            public override object Clone()
            {
                return new MyController();
            }
        }

        private class MyContext : IHttpClientContext
        {
            private const bool _secured = false;

            /// <summary>
            /// Using SSL or other encryption method.
            /// </summary>
            public bool Secured
            {
                get { return _secured; }
            }

            /// <summary>
            /// Using SSL or other encryption method.
            /// </summary>
            public bool IsSecured
            {
                get { return _secured; }
            }

            /// <summary>
            /// Disconnect from client
            /// </summary>
            /// <param name="error">error to report in the <see cref="Disconnected"/> event.</param>
            public void Disconnect(SocketError error)
            {
            }

            /// <summary>
            /// Send a response.
            /// </summary>
            /// <param name="httpVersion">Either HttpHelper.HTTP10 or HttpHelper.HTTP11</param>
            /// <param name="statusCode">http status code</param>
            /// <param name="reason">reason for the status code.</param>
            /// <param name="body">html body contents, can be null or empty.</param>
            /// <param name="contentType">A content type to return the body as, ie 'text/html' or 'text/plain', defaults to 'text/html' if null or empty</param>
            /// <exception cref="ArgumentException">If httpVersion is invalid.</exception>
            public void Respond(string httpVersion, HttpStatusCode statusCode, string reason, string body, string contentType)
            {
            }

            /// <summary>
            /// Send a response.
            /// </summary>
            /// <param name="httpVersion">Either HttpHelper.HTTP10 or HttpHelper.HTTP11</param>
            /// <param name="statusCode">http status code</param>
            /// <param name="reason">reason for the status code.</param>
            public void Respond(string httpVersion, HttpStatusCode statusCode, string reason)
            {
            }

            /// <summary>
            /// Send a response.
            /// </summary>
            /// <exception cref="ArgumentNullException"></exception>
            public void Respond(string body)
            {
            }

            /// <summary>
            /// send a whole buffer
            /// </summary>
            /// <param name="buffer">buffer to send</param>
            /// <exception cref="ArgumentNullException"></exception>
            public void Send(byte[] buffer)
            {
            }

            /// <summary>
            /// Send data using the stream
            /// </summary>
            /// <param name="buffer">Contains data to send</param>
            /// <param name="offset">Start position in buffer</param>
            /// <param name="size">number of bytes to send</param>
            /// <exception cref="ArgumentNullException"></exception>
            /// <exception cref="ArgumentOutOfRangeException"></exception>
            public void Send(byte[] buffer, int offset, int size)
            {
            }

            public void ShutupWarnings()
            {
                Disconnected(this, null);
                RequestReceived(this, null);
            }

            public event EventHandler<DisconnectedEventArgs> Disconnected;
            public event EventHandler<RequestEventArgs> RequestReceived;
        }
    }
}
