using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using HttpServer.Helpers.Implementations;

namespace HttpServer.Helpers
{
  /// <summary>
  /// Webhelper provides helpers for common tasks in HTML.
  /// </summary>
  public static class WebHelper
  {
    /// <summary>
    /// Used to let the website use different javascript libraries.
    /// Default is <see cref="PrototypeImp"/>
    /// </summary>
    public static JavascriptHelperImplementation JSImplementation = new PrototypeImp();

    /// <summary>
    /// Creates a link that invokes through ajax.
    /// </summary>
    /// <param name="url">url to fetch</param>
    /// <param name="title">link title</param>
    /// <param name="options">
    /// optional options in format "key, value, key, value". 
    /// Javascript options starts with ':'.
    /// </param>
    /// <returns>a link tag</returns>
    /// <example>
    /// WebHelper.AjaxRequest("/users/add/", "Add user", "method:", "post", "onclick", "validate('this');");
    /// </example>
    public static string AjaxRequest(string url, string title, params string[] options)
    {
      string text = JSImplementation.AjaxRequest("this.href", options) + "return false;";
      options = JSImplementation.RemoveJavascriptOptions(options);
      return BuildLink(url, title, options, "onclick", text);
    }

    /// <summary>
    /// Builds a link that updates an element with the fetched ajax content.
    /// </summary>
    /// <param name="url">Url to fetch content from</param>
    /// <param name="title">link title</param>
    /// <param name="targetId">html element to update with the results of the ajax request.</param>
    /// <param name="options">optional options in format "key, value, key, value"</param>
    /// <returns>A link tag.</returns>
    public static string AjaxUpdater(string url, string title, string targetId, params string[] options)
    {
      string text = JSImplementation.AjaxUpdater("this.href", targetId, options) + "return false;";
      options = JSImplementation.RemoveJavascriptOptions(options);
      return BuildLink(url, title, options, "onclick", text);
    }

    /// <summary>
    /// A link that pop ups a Dialog (overlay div)
    /// </summary>
    /// <param name="url">url to contents of dialog</param>
    /// <param name="title">link title</param>
    /// <param name="htmlAttributes">name/value of html attributes.</param>
    /// <returns>A "a"-tag that popups a dialog when clicked</returns>
    /// <example>
    /// WebHelper.DialogLink("/user/show/1", "show user", "onmouseover", "alert('booh!');");
    /// </example>
    public static string DialogLink(string url, string title, params string[] htmlAttributes)
    {
      return JSImplementation.DialogLink(url, title, htmlAttributes);
    }

    /// <summary>
    /// Create/Open a dialog box using ajax
    /// </summary>
    /// <param name="url"></param>
    /// <param name="title"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static string CreateDialog(string url, string title, params string[] parameters)
    {
      return JSImplementation.CreateDialog(url, title, parameters);
    }

    /// <summary>
    /// Close a javascript dialog window/div.
    /// </summary>
    /// <returns>javascript for closing a dialog.</returns>
    /// <see cref="DialogLink"/>
    public static string CloseDialog()
    {
      return JSImplementation.CloseDialog();
    }

    /// <summary>
    /// Create a &lt;form&gt; tag.
    /// </summary>
    /// <param name="name">name of form</param>
    /// <param name="action">action to invoke on submit</param>
    /// <param name="isAjax">form should be posted as ajax</param>
    /// <returns>html code</returns>
    /// <example>
    /// WebHelper.FormStart("frmLogin", "/user/login", Request.IsAjax);
    /// </example>
    [Obsolete("Moved to FormHelper")]
    public static string FormStart(string name, string action, bool isAjax)
    {
      string formStart = "<form method=\"post\" name=\"" + name + "\" id=\"" + name + "\" action=\"" + action +
          "\"";
      if (isAjax)
        return
            formStart +
                "onsubmit=\"" + JSImplementation.AjaxFormOnSubmit() + "\">";

      return formStart + ">";
    }

    /// <summary>
    /// Create a link tag.
    /// </summary>
    /// <param name="url">url to go to</param>
    /// <param name="title">link title (text that is displayed)</param>
    /// <param name="htmlAttributes">html attributes, name, value, name, value</param>
    /// <returns>html code</returns>
    /// <example>
    /// WebHelper.Link("/user/show/1", "Show user", "id", "showUser", "onclick", "return confirm('Are you shure?');");
    /// </example>
    public static string Link(string url, string title, params string[] htmlAttributes)
    {
      return BuildLink(url, title, htmlAttributes);
    }

    /// <summary>
    /// Build a link
    /// </summary>
    /// <param name="url">url to go to.</param>
    /// <param name="title">title of link (displayed text)</param>
    /// <param name="htmlAttributes">extra html attributes.</param>
    /// <returns>a complete link</returns>
    public static string BuildLink(string url, string title, params string[] htmlAttributes)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("<a href=\"");
      sb.Append(url);
      sb.Append("\"");
      GenerateHtmlAttributes(sb, htmlAttributes);
      sb.Append(">");
      sb.Append(title);
      sb.Append("</a>");
      return sb.ToString();
    }

    /// <summary>
    /// Build a link
    /// </summary>
    /// <param name="url">url to go to.</param>
    /// <param name="title">title of link (displayed text)</param>
    /// <param name="htmlAttributes">extra html attributes.</param>
    /// <returns>a complete link</returns>
    /// <param name="options">more options</param>
    internal static string BuildLink(string url, string title, string[] htmlAttributes, params string[] options)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("<a href=\"");
      sb.Append(url);
      sb.Append("\"");
      GenerateHtmlAttributes(sb, htmlAttributes, options);
      sb.Append(">");
      sb.Append(title);
      sb.Append("</a>");
      return sb.ToString();
    }

    /// <summary>
    /// Obsolete
    /// </summary>
    /// <param name="name">Obsolete</param>
    /// <param name="collection">Obsolete</param>
    /// <param name="getIdTitle">Obsolete</param>
    /// <param name="selectedValue">Obsolete</param>
    /// <param name="firstEmpty">Obsolete</param>
    /// <returns>Obsolete</returns>
    [Obsolete("Moved to FormHelper")]
    public static string Select(
        string name, ICollection collection, GetIdTitle getIdTitle, object selectedValue, bool firstEmpty)
    {
      return FormHelper.Select(name, name, collection, getIdTitle, selectedValue, firstEmpty);
    }

    /// <summary>
    /// Obsolete
    /// </summary>
    /// <param name="name">Obsolete</param>
    /// <param name="id">Obsolete</param>
    /// <param name="collection">Obsolete</param>
    /// <param name="getIdTitle">Obsolete</param>
    /// <param name="selectedValue">Obsolete</param>
    /// <param name="firstEmpty">Obsolete</param>
    /// <returns>Obsolete</returns>
    [Obsolete("Moved to FormHelper")]
    public static string Select(
        string name, string id, ICollection collection, GetIdTitle getIdTitle, object selectedValue, bool firstEmpty)
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("<select name=\"" + name + "\" id=\"" + id + "\">");
      if (firstEmpty)
        sb.AppendLine("<option value=\"\">&nbsp;</option>");


      foreach (object o in collection)
      {
        object value;
        string title;
        getIdTitle(o, out value, out title);
        sb.Append("<option value=\"");
        if (value != null)
          sb.Append(value);
        sb.Append("\"");

        if (value != null && value.Equals(selectedValue))
          sb.Append(" selected=\"selected\"");

        sb.Append(">");

        if (title == null)
          sb.AppendLine("&nbsp;</option>");
        else
        {
          sb.Append(value);
          sb.AppendLine("</option>");
        }
      }

      sb.AppendLine("</select>");
      return sb.ToString();
    }

    /// <summary>
    /// Render errors into a UL with class "errors"
    /// </summary>
    /// <param name="className">class used by UL-tag.</param>
    /// <param name="theList">items to list</param>
    /// <returns>an unordered html list.</returns>
    public static string List(IEnumerable<object> theList, string className)
    {
      if (theList.GetEnumerator().Current == null)
        return string.Empty;

      StringBuilder sb = new StringBuilder();
      sb.Append("<ul class=\"");
      sb.Append(className);
      sb.AppendLine("\">");
      foreach (string error in theList)
      {
        sb.Append("<li>");
        sb.Append(error);
        sb.AppendLine("</li>");
      }

      sb.AppendLine("</ul>");
      return sb.ToString();
    }


    /// <summary>
    /// Render errors into a UL with class "errors"
    /// </summary>
    /// <param name="className">class used by UL-tag.</param>
    /// <param name="theList">items to list</param>
    /// <returns>an unordered html list.</returns>
    public static string List(NameValueCollection theList, string className)
    {
      if (theList.Count == 0)
        return string.Empty;

      StringBuilder sb = new StringBuilder();
      sb.Append("<ul class=\"");
      sb.Append(className);
      sb.AppendLine("\">");
      foreach (string key in theList.AllKeys)
      {
        sb.Append("<li>");
        sb.Append(theList[key]);
        sb.AppendLine("</li>");
      }

      sb.AppendLine("</ul>");
      return sb.ToString();
    }


    /// <summary>
    /// Render errors into a UL with class "errors"
    /// </summary>
    /// <param name="errors"></param>
    /// <returns></returns>
    public static string Errors(NameValueCollection errors)
    {
      return List(errors, "error");
    }

    /// <summary>
    /// Generates a list with html attributes.
    /// </summary>
    /// <param name="sb">StringBuilder that the options should be added to.</param>
    /// <param name="firstOptions">attributes set by user.</param>
    /// <param name="secondOptions">attributes set by any of the helper classes.</param>
    internal static void GenerateHtmlAttributes(StringBuilder sb, string[] firstOptions, string[] secondOptions)
    {
      if (firstOptions.Length%2 == 1)
        throw new ArgumentException("Options must be in pairs [key,value]", "firstOptions");
      if (secondOptions.Length%2 == 1)
        throw new ArgumentException("Options must be in pairs [key,value]", "secondOptions");

      Dictionary<string, List<string>> options = new Dictionary<string, List<string>>();
      for (int i = 0; i < firstOptions.Length; i += 2)
      {
        if (!options.ContainsKey(firstOptions[i]))
          options.Add(firstOptions[i], new List<string>());
        options[firstOptions[i]].Add(firstOptions[i + 1]);
      }
      for (int i = 0; i < secondOptions.Length; i += 2)
      {
        if (!options.ContainsKey(secondOptions[i]))
          options.Add(secondOptions[i], new List<string>());
        options[secondOptions[i]].Add(secondOptions[i + 1]);
      }

      foreach (KeyValuePair<string, List<string>> option in options)
      {
        sb.Append(" ");
        sb.Append(option.Key);
        sb.Append("=\"");
        for (int i = 0; i < option.Value.Count; ++i)
          sb.Append(i != option.Value.Count - 1 ? option.Value[i] + " " : option.Value[i]);

        sb.Append("\" ");
      }
    }

    /// <summary>
    /// Generates a list with html attributes.
    /// </summary>
    /// <param name="sb">StringBuilder that the options should be added to.</param>
    /// <param name="options"></param>
    internal static void GenerateHtmlAttributes(StringBuilder sb, string[] options)
    {
      for (int i = 0; i < options.Length; i += 2)
      {
        if (string.IsNullOrEmpty(options[i]) || options[i][options[i].Length - 1] == ':')
          continue;

        sb.Append(" ");
        sb.Append(options[i]);
        sb.Append("=\"");
        sb.Append(options[i + 1]);
        sb.Append("\"");
      }
    }
  }

  /// <summary>
  /// Purpose of this class is to create a javascript toolkit independent javascript helper.
  /// </summary>
  public abstract class JavascriptHelperImplementation
  {
    /// <summary>
    /// Generates a list with JS options.
    /// </summary>
    /// <param name="sb">StringBuilder that the options should be added to.</param>
    /// <param name="options">the javascript options. name, value pairs. each string value should be escaped by YOU!</param>
    /// <param name="startWithComma">true if we should start with a comma.</param>
    internal virtual void GenerateOptions(StringBuilder sb, string[] options, bool startWithComma)
    {
      if (startWithComma)
        sb.Append(", ");

      long len = sb.Length;
      for (int i = 0; i < options.Length; i += 2)
      {
        if (string.IsNullOrEmpty(options[i]) || options[i][options[i].Length - 1] != ':')
          continue;

        sb.Append(options[i]);
        sb.Append(" ");
        sb.Append(options[i + 1]);
        sb.Append(", ");
      }

      // remove the last comma.
      if (sb.Length != len || startWithComma)
        sb.Length -= 2;
    }

    /// <summary>
    /// Removes any javascript parameters from an array of parameters
    /// </summary>
    /// <param name="options">The array of parameters to remove javascript params from</param>
    /// <returns>An array of html parameters</returns>
    internal virtual string[] RemoveJavascriptOptions(string[] options)
    {
      if (options == null)
        throw new ArgumentNullException("options");
      if (options.Length%2 == 1)
        throw new ArgumentException("Options must be in pairs [key,value]", "options");

      List<string> saveParams = new List<string>();
      for (int i = 0; i < options.Length; i += 2)
      {
        if (!options[i].TrimEnd().EndsWith(":"))
        {
          saveParams.Add(options[i]);
          saveParams.Add(options[i + 1]);
        }
      }

      return saveParams.ToArray();
    }

    /// <summary>
    /// javascript action that should be added to the "onsubmit" event in the form tag.
    /// </summary>
    /// <returns></returns>
    /// <remarks>All javascript option names should end with colon.</remarks>
    /// <example>
    /// <code>
    /// JSHelper.AjaxRequest("/user/show/1", "onsuccess:", "$('userInfo').update(result);");
    /// </code>
    /// </example>
    public abstract string AjaxFormOnSubmit(params string[] options);

    /// <summary>
    /// Requests a url through ajax
    /// </summary>
    /// <param name="url">url to fetch</param>
    /// <param name="options">optional options in format "key, value, key, value", used in JS request object.</param>
    /// <returns>a link tag</returns>
    /// <remarks>All javascript option names should end with colon.</remarks>
    /// <example>
    /// <code>
    /// JSHelper.AjaxRequest("/user/show/1", "onsuccess:", "$('userInfo').update(result);");
    /// </code>
    /// </example>
    public abstract string AjaxRequest(string url, params string[] options);

    /// <summary>
    /// Ajax requests that updates an element with
    /// the fetched content
    /// </summary>
    /// <param name="url">Url to fetch content from</param>
    /// <param name="targetId">element to update</param>
    /// <param name="options">optional options in format "key, value, key, value", used in JS updater object.</param>
    /// <returns>A link tag.</returns>
    /// <remarks>All javascript option names should end with colon.</remarks>
    /// <example>
    /// <code>
    /// JSHelper.AjaxUpdater("/user/show/1", "userInfo", "onsuccess:", "alert('Successful!');");
    /// </code>
    /// </example>
    public abstract string AjaxUpdater(string url, string targetId, params string[] options);

    /// <summary>
    /// A link that pop ups a Dialog (overlay div)
    /// </summary>
    /// <param name="url">url to contents of dialog</param>
    /// <param name="title">link title</param>
    /// <returns>A "a"-tag that popups a dialog when clicked</returns>
    /// <param name="htmlAttributes">name/value of html attributes</param>
    /// <example>
    /// WebHelper.DialogLink("/user/show/1", "show user", "onmouseover", "alert('booh!');");
    /// </example>
    public abstract string DialogLink(string url, string title, params string[] htmlAttributes);

    /// <summary>
    /// Close a javascript dialog window/div.
    /// </summary>
    /// <returns>javascript for closing a dialog.</returns>
    /// <see cref="DialogLink"/>
    public abstract string CloseDialog();

    /// <summary>
    /// Creates a new modal dialog window
    /// </summary>
    /// <param name="url">url to open in window.</param>
    /// <param name="title">window title (may not be supported by all js implementations)</param>
    /// <param name="options"></param>
    /// <returns></returns>
    public abstract string CreateDialog(string url, string title, params string[] options);
  }
}