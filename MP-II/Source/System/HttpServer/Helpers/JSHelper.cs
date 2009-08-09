namespace HttpServer.Helpers
{
  /// <summary>
  /// Will contain helper functions for javascript.
  /// </summary>
  public static class JSHelper
  {
    /// <summary>
    /// Requests a url through ajax
    /// </summary>
    /// <param name="url">url to fetch. Url is NOT enclosed in quotes by the implementation. You need to do that yourself.</param>
    /// <param name="options">optional options in format "key, value, key, value", used in JS request object. All keys should end with colon.</param>
    /// <returns>a link tag</returns>
    /// <remarks>onclick attribute is used by this method.</remarks>
    /// <example>
    /// <code>
    /// // plain text
    /// JSHelper.AjaxRequest("'/user/show/1'");
    /// 
    /// // ajax request using this.href
    /// string link = "&lt;a href=\"/user/call/1\" onclick=\"" + JSHelper.AjaxRequest("this.href") + "/&lt;call user&lt;/a&gt;";
    /// </code>
    /// </example>
    public static string AjaxRequest(string url, params string[] options)
    {
      return WebHelper.JSImplementation.AjaxRequest(url, options);
    }


    /// <summary>
    /// Ajax requests that updates an element with
    /// the fetched content
    /// </summary>
    /// <param name="url">url to fetch. Url is NOT enclosed in quotes by the implementation. You need to do that yourself.</param>
    /// <param name="targetId">element to update</param>
    /// <param name="options">options in format "key, value, key, value". All keys should end with colon.</param>
    /// <returns>A link tag.</returns>
    /// <example>
    /// <code>
    /// JSHelper.AjaxUpdater("'/user/show/1'", "user", "onsuccess:", "alert('hello');", "asynchronous:", "true");
    /// </code>
    /// </example>
    public static string AjaxUpdater(string url, string targetId, params string[] options)
    {
      return WebHelper.JSImplementation.AjaxUpdater(url, targetId, options);
    }

    /// <summary>
    /// Opens contents in a dialog window.
    /// </summary>
    /// <param name="url">url to contents of dialog</param>
    /// <param name="title">link title</param>
    /// <param name="options">name, value, name, value, all parameter names should end with colon.</param>
    public static string CreateDialog(string url, string title, params string[] options)
    {
      return WebHelper.JSImplementation.CreateDialog(url, title, options);
    }

    /// <summary>
    /// Close a javascript dialog window/div.
    /// </summary>
    /// <returns>javascript for closing a dialog.</returns>
    /// <see cref="CreateDialog" />
    public static string CloseDialog()
    {
      return WebHelper.JSImplementation.CloseDialog();
    }
  }
}