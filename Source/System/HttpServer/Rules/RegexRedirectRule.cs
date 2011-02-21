using System;
using System.Text.RegularExpressions;

namespace HttpServer.Rules
{
  /// <summary>
  /// Class to make dynamic binding of redirects. Instead of having to specify a number of similar redirect rules
  /// a regular expression can be used to identify redirect URLs and their targets.
  /// </summary>
  /// <example>
  /// <![CDATA[
  /// new RegexRedirectRule("/(?<target>[a-z0-9]+)", "/users/${target}?find=true", RegexOptions.IgnoreCase)
  /// ]]>
  /// </example>
  public class RegexRedirectRule : RedirectRule
  {
    private readonly Regex _matchUrl = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexRedirectRule"/> class.
    /// </summary>
    /// <param name="fromUrlExpression">Expression to match URL</param>
    /// <param name="toUrlExpression">Expression to generate URL</param>
    /// <example>
    /// <![CDATA[
    /// server.Add(new RegexRedirectRule("/(?<first>[a-zA-Z0-9]+)", "/user/${first}"));
    /// Result of ie. /employee1 will then be /user/employee1
    /// ]]>
    /// </example>
    public RegexRedirectRule(string fromUrlExpression, string toUrlExpression)
        : this(fromUrlExpression, toUrlExpression, RegexOptions.None, true)
    {
    }

    #region public RegexRedirectRule(string fromUrlExpression, string toUrlExpression, RegexOptions options)

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexRedirectRule"/> class.
    /// </summary>
    /// <param name="fromUrlExpression">Expression to match URL</param>
    /// <param name="toUrlExpression">Expression to generate URL</param>
    /// <param name="options">Regular expression options to use, can be null</param>
    /// <example>
    /// <![CDATA[
    /// server.Add(new RegexRedirectRule("/(?<first>[a-zA-Z0-9]+)", "/user/{first}", RegexOptions.IgnoreCase));
    /// Result of ie. /employee1 will then be /user/employee1
    /// ]]>
    /// </example>
    public RegexRedirectRule(string fromUrlExpression, string toUrlExpression, RegexOptions options)
        : this(fromUrlExpression, toUrlExpression, options, true)
    {
    }

    #endregion

    #region public RegexRedirectRule(string fromUrlExpression, string toUrlExpression, RegexOptions options, bool shouldRedirect)

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexRedirectRule"/> class.
    /// </summary>
    /// <param name="fromUrlExpression">Expression to match URL</param>
    /// <param name="toUrlExpression">Expression to generate URL</param>
    /// <param name="options">Regular expression options to apply</param>
    /// <param name="shouldRedirect"><c>true</c> if request should be redirected, <c>false</c> if the request URI should be replaced.</param>
    /// <example>
    /// <![CDATA[
    /// server.Add(new RegexRedirectRule("/(?<first>[a-zA-Z0-9]+)", "/user/${first}", RegexOptions.None));
    /// Result of ie. /employee1 will then be /user/employee1
    /// ]]>
    /// </example>
    /// <exception cref="ArgumentNullException">Argument is null.</exception>
    /// <seealso cref="RedirectRule.ShouldRedirect"/>
    public RegexRedirectRule(
        string fromUrlExpression, string toUrlExpression, RegexOptions options, bool shouldRedirect) :
            base(fromUrlExpression, toUrlExpression, shouldRedirect)
    {
      if (string.IsNullOrEmpty(fromUrlExpression))
        throw new ArgumentNullException("fromUrlExpression");
      if (string.IsNullOrEmpty(toUrlExpression))
        throw new ArgumentNullException("toUrlExpression");

      _matchUrl = new Regex(fromUrlExpression, options);
    }

    #endregion

    /// <summary>
    /// Process the incoming request.
    /// </summary>
    /// <param name="request">incoming HTTP request</param>
    /// <param name="response">outgoing HTTP response</param>
    /// <returns>true if response should be sent to the browser directly (no other rules or modules will be processed).</returns>
    /// <remarks>
    /// returning true means that no modules will get the request. Returning true is typically being done
    /// for redirects.
    /// </remarks>
    /// <exception cref="ArgumentNullException">If request or response is null</exception>
    public override bool Process(IHttpRequest request, IHttpResponse response)
    {
      if (request == null)
        throw new ArgumentNullException("request");
      if (response == null)
        throw new ArgumentNullException("response");

      // If a match is found
      if (_matchUrl.IsMatch(request.Uri.AbsolutePath))
      {
        // Return the replace result
        string resultUrl = _matchUrl.Replace(request.Uri.AbsolutePath, ToUrl);
        if (!ShouldRedirect)
        {
          request.Uri = new Uri(request.Uri, resultUrl);
          return false;
        }

        response.Redirect(resultUrl);
        return true;
      }

      return false;
    }
  }
}