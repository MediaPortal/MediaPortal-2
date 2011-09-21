using System;
using System.Collections.Specialized;
using System.IO;
using HttpServer.Exceptions;
using HttpServer.FormDecoders;

namespace HttpServer
{
  /// <summary>
  /// Contains server side HTTP request information.
  /// </summary>
  public interface IHttpRequest : ICloneable
  {
    /// <summary>
    /// Gets kind of types accepted by the client.
    /// </summary>
    string[] AcceptTypes { get; }

    /// <summary>
    /// Gets or sets body stream.
    /// </summary>
    Stream Body { get; set; }

    /// <summary>
    /// Gets whether the body is complete.
    /// </summary>
    bool BodyIsComplete { get; }

    /// <summary>
    /// Gets or sets kind of connection used for the session.
    /// </summary>
    ConnectionType Connection { get; set; }

    /// <summary>
    /// Gets or sets number of bytes in the body.
    /// </summary>
    int ContentLength { get; set; }

    /// <summary>
    /// Gets cookies that was sent with the request.
    /// </summary>
    RequestCookies Cookies { get; }

    /// <summary>
    /// Gets form parameters.
    /// </summary>
    HttpForm Form { get; }

    /// <summary>
    /// Gets headers sent by the client.
    /// </summary>
    NameValueCollection Headers { get; }

    /// <summary>
    /// Gets or sets version of HTTP protocol that's used. 
    /// </summary>
    /// <remarks>
    /// Probably <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/>.
    /// </remarks>
    /// <seealso cref="HttpHelper"/>
    string HttpVersion { get; set; }

    /// <summary>
    /// Gets whether the request was made by Ajax (Asynchronous JavaScript)
    /// </summary>
    bool IsAjax { get; }

    /// <summary>
    /// Gets or sets requested method.
    /// </summary>
    /// <remarks>
    /// Will always be in upper case.
    /// </remarks>
    /// <see cref="Method"/>
    string Method { get; set; }

    /// <summary>
    /// Gets parameter from <see cref="QueryString"/> or <see cref="Form"/>.
    /// </summary>
    HttpParam Param { get; }

    /// <summary>
    /// Gets variables sent in the query string
    /// </summary>
    HttpInput QueryString { get; }

    /// <summary>
    /// Gets or sets requested URI.
    /// </summary>
    Uri Uri { get; set; }

    /// <summary>
    /// Gets URI absolute path divided into parts.
    /// </summary>
    /// <example>
    /// // URI is: http://gauffin.com/code/tiny/
    /// Console.WriteLine(request.UriParts[0]); // result: code
    /// Console.WriteLine(request.UriParts[1]); // result: tiny
    /// </example>
    /// <remarks>
    /// If you're using controllers than the first part is controller name,
    /// the second part is method name and the third part is Id property.
    /// </remarks>
    /// <seealso cref="Uri"/>
    string[] UriParts { get; }

    /// <summary>
    /// Gets or sets path and query.
    /// </summary>
    /// <see cref="Uri"/>
    /// <remarks>
    /// Are only used during request parsing. Cannot be set after "Host" header have been
    /// added.
    /// </remarks>
    string UriPath { get; set; }

    /// <summary>
    /// Called during parsing of a <see cref="IHttpRequest"/>.
    /// </summary>
    /// <param name="name">Name of the header, should not be URL encoded</param>
    /// <param name="value">Value of the header, should not be URL encoded</param>
    /// <exception cref="BadRequestException">If a header is incorrect.</exception>
    void AddHeader(string name, string value);

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
    int AddToBody(byte[] bytes, int offset, int length);

    /// <summary>
    /// Clear everything in the request
    /// </summary>
    void Clear();

    /// <summary>
    /// Decode body into a form.
    /// </summary>
    /// <param name="providers">A list with form decoders.</param>
    /// <exception cref="InvalidDataException">If body contents is not valid for the chosen decoder.</exception>
    /// <exception cref="InvalidOperationException">If body is still being transferred.</exception>
    void DecodeBody(FormDecoderProvider providers);

    /// <summary>
    /// Sets the cookies.
    /// </summary>
    /// <param name="cookies">The cookies.</param>
    void SetCookies(RequestCookies cookies);

    /// <summary>
    /// Create a response object.
    /// </summary>
    /// <param name="context">Context for the connected client.</param>
    /// <returns>A new <see cref="IHttpResponse"/>.</returns>
    IHttpResponse CreateResponse(IHttpClientContext context);
  }
}