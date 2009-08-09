namespace HttpServer
{
  /// <summary>
  /// Contains all HTTP Methods (according to the HTTP 1.1 specification)
  /// <para>
  /// See: http://www.w3.org/Protocols/rfc2616/rfc2616-sec9.html
  /// </para>
  /// </summary>
  public static class Method
  {
    /// <summary>
    /// The DELETE method requests that the origin server delete the resource identified by the Request-URI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method MAY be overridden by human intervention (or other means) on the origin server. 
    /// The client cannot be guaranteed that the operation has been carried out, even if the status code 
    /// returned from the origin server indicates that the action has been completed successfully. 
    /// </para>
    /// <para>
    /// However, the server SHOULD NOT indicate success unless, at the time the response is given, 
    /// it intends to delete the resource or move it to an inaccessible location.
    /// </para>
    /// <para>
    /// A successful response SHOULD be 200 (OK) if the response includes an entity describing the status, 
    /// 202 (Accepted) if the action has not yet been enacted, 
    /// or 204 (No Content) if the action has been enacted but the response does not include an entity.
    /// </para>
    /// <para>
    /// If the request passes through a cache and the Request-URI identifies one or more currently cached entities, 
    /// those entries SHOULD be treated as stale. Responses to this method are not cacheable. 
    /// </para>
    /// </remarks>
    public const string Delete = "DELETE";

    /// <summary>
    /// The GET method means retrieve whatever information (in the form of an entity) is identified by the Request-URI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the Request-URI refers to a data-producing process, it is the produced data which shall be returned as the 
    /// entity in the response and not the source text of the process, unless that text happens to be the output of the process.
    /// </para>
    /// <para>
    /// The semantics of the GET method change to a "conditional GET" if the request message includes an 
    /// If-Modified-Since, If-Unmodified-Since, If-Match, If-None-Match, or If-Range header field. 
    /// A conditional GET method requests that the entity be transferred only under the circumstances described 
    /// by the conditional header field(s). The conditional GET method is intended to reduce unnecessary network 
    /// usage by allowing cached entities to be refreshed without requiring multiple requests or transferring 
    /// data already held by the client.
    /// </para>
    /// </remarks>
    public const string Get = "GET";

    /// <summary>
    /// The HEAD method is identical to GET except that the server MUST NOT return a message-body in the response. 
    /// </summary>
    /// <remarks>
    /// The meta information contained in the HTTP headers in response to a HEAD request SHOULD be identical to the
    ///  information sent in response to a GET request. This method can be used for obtaining meta information about 
    /// the entity implied by the request without transferring the entity-body itself. 
    /// 
    /// This method is often used for testing hypertext links for validity, accessibility, and recent modification.
    /// </remarks>
    public const string Header = "HEAD";

    /// <summary>
    /// <para>The OPTIONS method represents a request for information about the communication options available on the request/response chain identified by the Request-URI.</para>
    /// </summary>
    /// <remarks>
    /// <para>This method allows the client to determine the options and/or requirements associated with a resource, or the capabilities of a server, without implying a resource action or initiating a resource retrieval.</para>
    /// </remarks>
    public const string Options = "OPTIONS";

    /// <summary>
    /// The POST method is used to request that the origin server accept the entity enclosed 
    /// in the request as a new subordinate of the resource identified by the Request-URI in the Request-Line.
    /// </summary>
    /// <remarks>
    /// POST is designed to allow a uniform method to cover the following functions:
    /// <list type="bullet">
    /// <item>
    /// Annotation of existing resources;
    /// </item><item>
    /// Posting a message to a bulletin board, newsgroup, mailing list, or similar group of articles;
    /// </item><item>
    /// Providing a block of data, such as the result of submitting a form, to a data-handling process;
    /// </item><item>
    /// Extending a database through an append operation.
    /// </item>
    /// </list>
    /// <para>
    /// If a resource has been created on the origin server, the response SHOULD be 201 (Created) and 
    /// contain an entity which describes the status of the request and refers to the new resource, and a 
    /// Location header (see section 14.30).
    /// </para>
    /// <para>
    ///  The action performed by the POST method might not result in a resource that can be identified by a URI. 
    /// In this case, either 200 (OK) or 204 (No Content) is the appropriate response status, depending on 
    /// whether or not the response includes an entity that describes the result.
    /// </para><para>
    /// Responses to this method are not cacheable, unless the response includes appropriate Cache-Control 
    /// or Expires header fields. However, the 303 (See Other) response can be used to direct the user agent 
    /// to retrieve a cacheable resource. 
    /// </para>
    /// </remarks>
    public const string Post = "POST";

    /// <summary>
    /// The PUT method requests that the enclosed entity be stored under the supplied Request-URI.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// If the Request-URI refers to an already existing resource, the enclosed entity SHOULD be considered as a 
    /// modified version of the one residing on the origin server. 
    /// </item><item>
    /// If the Request-URI does not point to an existing resource, and that URI is capable of being defined as a new 
    /// resource by the requesting user agent, the origin server can create the resource with that URI. 
    /// </item><item>
    /// If a new resource is created, the origin server MUST inform the user agent via the 201 (Created) response. 
    /// </item><item>
    /// If an existing resource is modified, either the 200 (OK) or 204 (No Content) response codes SHOULD be sent to 
    /// indicate successful completion of the request. 
    /// </item><item>
    /// If the resource could not be created or modified with the Request-URI, an appropriate error response SHOULD be 
    /// given that reflects the nature of the problem. 
    /// </item>
    /// </list>
    /// <para>
    /// The recipient of the entity MUST NOT ignore any Content-* (e.g. Content-Range) headers that it does not 
    /// understand or implement and MUST return a 501 (Not Implemented) response in such cases.
    /// </para>
    /// </remarks>
    public const string Put = "PUT";

    /// <summary>
    /// The TRACE method is used to invoke a remote, application-layer loop- back of the request message.
    /// </summary>
    public const string Trace = "TRACE";
  }
}