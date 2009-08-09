using System;

namespace HttpServer.Rules
{
    /// <summary>
    /// Rules are used to perform operations before a request is being handled.
    /// Rules can be used to create routing etc.
    /// </summary>
    public interface Rule
    {
        /// <summary>
        /// Process the incoming request.
        /// </summary>
        /// <param name="request">incoming http request</param>
        /// <param name="response">outgoing http response</param>
        /// <returns>true if response should be sent to the browser directly (no other rules or modules will be processed).</returns>
        /// <remarks>
        /// returning true means that no modules will get the request. Returning true is typically being done
        /// for redirects.
        /// </remarks>
        /// <exception cref="ArgumentNullException">If request or response is null.</exception>
        bool Process(HttpRequest request, HttpResponse response);
    }
}