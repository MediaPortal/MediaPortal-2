using System.Collections.Generic;
using System.Text;

namespace HttpServer.Helpers.Implementations
{
  /// <summary>
  /// PrototypeJS implementation of the javascript functions.
  /// </summary>
  public class PrototypeImp : JavascriptHelperImplementation
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
    public override string AjaxRequest(string url, params string[] options)
    {
      // For each parameter ending with : push it into the Ajax script as a javascript parameter
      StringBuilder sb = new StringBuilder();
      sb.Append("new Ajax.Request(");
      sb.Append(url);
      sb.Append(", { ");
      bool startWithComma = false;
      if (!Contains(options, "method:"))
      {
        sb.Append("method: 'get'");
        startWithComma = true;
      }
      if (!Contains(options, "evalScripts:"))
      {
        sb.Append(startWithComma ? ",evalScripts: true" : "evalScripts: true");
        startWithComma = true;
      }
      GenerateOptions(sb, options, startWithComma);
      sb.Append(" });");
      return sb.ToString();
    }

    /// <summary>
    /// Determins if a list of strings contains a specific value
    /// </summary>
    /// <param name="options">options to check in</param>
    /// <param name="value">value to find</param>
    /// <returns>true if value was found</returns>
    /// <remarks>case insensitive</remarks>
    private static bool Contains(IEnumerable<string> options, string value)
    {
      foreach (string s in options)
      {
        if (string.Compare(value, s, true) == 0)
          return true;
      }

      return false;
    }

    /// <summary>
    /// Ajax requests that updates an element with
    /// the fetched content
    /// </summary>
    /// <param name="url">URL to fetch. URL is NOT enclosed in quotes by the implementation. You need to do that yourself.</param>
    /// <param name="targetId">element to update</param>
    /// <param name="options">options in format "key, value, key, value". All keys should end with colon.</param>
    /// <returns>A link tag.</returns>
    /// <example>
    /// <code>
    /// JSHelper.AjaxUpdater("'/user/show/1'", "user", "onsuccess:", "alert('hello');", "asynchronous:", "true");
    /// </code>
    /// </example>
    public override string AjaxUpdater(string url, string targetId, params string[] options)
    {
      // For each parameter ending with : push it into the Ajax script as a javascript parameter
      StringBuilder sb = new StringBuilder();
      sb.Append("new Ajax.Updater('");
      sb.Append(targetId);
      sb.Append("', ");
      sb.Append(url);
      sb.Append(", { ");
      bool startWithComma = false;
      if (!Contains(options, "method:"))
      {
        sb.Append("method: 'get'");
        startWithComma = true;
      }
      if (!Contains(options, "evalScripts:"))
      {
        sb.Append(startWithComma ? ",evalScripts: true" : "evalScripts: true");
        startWithComma = true;
      }
      GenerateOptions(sb, options, startWithComma);
      sb.Append(" });");
      return sb.ToString();
    }

    /// <summary>
    /// A link that pop ups a Dialog (overlay div)
    /// </summary>
    /// <param name="url">URL to contents of dialog</param>
    /// <param name="title">link title</param>
    /// <param name="htmlAttributes">name, value, name, value</param>
    /// <returns>
    /// A "a"-tag that popups a dialog when clicked
    /// </returns>
    /// <remarks><para>Requires Control.Modal found here: http://livepipe.net/projects/control_modal/</para>
    /// And the following JavaScript (load it in application.js):
    /// <code>
    /// Event.observe(window, 'load',
    ///   function() {
    ///     document.getElementsByClassName('modal').each(function(link){  new Control.Modal(link);  });
    ///   }
    /// );
    /// </code>
    /// </remarks>
    /// <example>
    /// WebHelper.DialogLink("/user/show/1", "show user", "onmouseover", "alert('booh!');");
    /// </example>
    public override string DialogLink(string url, string title, params string[] htmlAttributes)
    {
      return WebHelper.BuildLink(url, title, htmlAttributes, "class", "modal");
    }

    #region JavascriptImplementation Members

    /// <summary>
    /// create a modal dialog (usually using DIVs)
    /// </summary>
    /// <param name="url">url to fetch</param>
    /// <param name="title">dialog title</param>
    /// <param name="options">javascript/html attributes. javascript options ends with colon ':'.</param>
    /// <returns></returns>
    public override string CreateDialog(string url, string title, params string[] options)
    {
      return string.Format("new Control.Modal('{0}');", url);
    }

    /// <summary>
    /// Close a javascript dialog window/div.
    /// </summary>
    /// <returns>javascript for closing a dialog.</returns>
    /// <see cref="DialogLink" />
    public override string CloseDialog()
    {
      return "Control.Modal.close();";
    }


    /// <summary>
    /// javascript action that should be added to the "onsubmit" event in the form tag.
    /// </summary>
    /// <param name="options">remember to encapsulate strings in ''</param>
    /// <returns></returns>
    /// <remarks>All javascript option names should end with colon.</remarks>
    /// <example>
    /// 	<code>
    /// JSHelper.AjaxRequest("/user/show/1", "onsuccess:", "$('userInfo').update(result);");
    /// </code>
    /// </example>
    public override string AjaxFormOnSubmit(params string[] options)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("new Ajax.Request(this.action, { parameters: Form.serialize(this)");

      // copy all javascript options
      string[] theOptions = new string[options.Length + 6];
      //options.CopyTo(theOptions, 0); todo: is this faster or slower?
      for (int i = 0; i < options.Length; i += 2)
      {
        if (options[i][options[i].Length - 1] == ':')
        {
          theOptions[i] = options[i];
          theOptions[i + 1] = options[i + 1];
        }
      }

      int index = options.Length;
      theOptions[index++] = "method:";
      theOptions[index++] = "'post'";
      theOptions[index++] = "asynchronous:";
      theOptions[index++] = "true";
      theOptions[index++] = "evalScripts:";
      theOptions[index] = "true";

      GenerateOptions(sb, theOptions, true);
      sb.Append(" });");
      return sb.ToString();
    }

    #endregion
  }
}