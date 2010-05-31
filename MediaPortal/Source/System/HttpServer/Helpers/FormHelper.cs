using System;
using System.Collections;
using System.Text;

namespace HttpServer.Helpers
{
  /// <summary>
  /// Helpers making it easier to work with forms.
  /// </summary>
  /// <seealso cref="ObjectForm"/>
  public static class FormHelper
  {
    /// <summary>
    /// Used to let the website use different JavaScript libraries.
    /// Default is <see cref="Implementations.PrototypeImp"/>
    /// </summary>
    public static JavascriptHelperImplementation JSImplementation = WebHelper.JSImplementation;


    /// <summary>
    /// Create a &lt;form&gt; tag.
    /// </summary>
    /// <param name="id">name of form</param>
    /// <param name="action">action to invoke on submit</param>
    /// <param name="isAjax">form should be posted as Ajax</param>
    /// <returns>HTML code</returns>
    /// <example>
    /// <code>
    /// // without options
    /// WebHelper.FormStart("frmLogin", "/user/login", Request.IsAjax);
    /// 
    /// // with options
    /// WebHelper.FormStart("frmLogin", "/user/login", Request.IsAjax, "style", "display:inline", "class", "greenForm");
    /// </code>
    /// </example>
    /// <param name="options">HTML attributes or JavaScript options.</param>
    /// <remarks>Method will ALWAYS be POST.</remarks>
    /// <exception cref="ArgumentException">options must consist of name, value, name, value</exception>
    public static string Start(string id, string action, bool isAjax, params string[] options)
    {
      Check.Require(id, "id");
      Check.NotEmpty(action, "action");
      if (options == null || options.Length%2 != 0)
        throw new ArgumentException("options must consist of name, value, name, value");

      StringBuilder sb = new StringBuilder();
      sb.Append("<form action=\"");
      sb.Append(action);
      sb.Append("\"");

      if (isAjax)
      {
        string onsubmit = JSImplementation.AjaxFormOnSubmit(options) + "return false;";
        WebHelper.GenerateHtmlAttributes(sb, options, new[] {"onsubmit", onsubmit, "id", id, "method", "post"});
      }
      else
        WebHelper.GenerateHtmlAttributes(sb, options, new[] {"id", id, "method", "post"});

      sb.Append(">");
      return sb.ToString();
    }

    /// <summary>
    /// Creates a select list with the values in a collection.
    /// </summary>
    /// <param name="name">Name of the SELECT-tag</param>
    /// <param name="collection">collection used to generate options.</param>
    /// <param name="getIdTitle">delegate used to return id and title from objects.</param>
    /// <param name="selectedValue">value that should be marked as selected.</param>
    /// <param name="firstEmpty">First row should contain an empty value.</param>
    /// <returns>string containing a SELECT-tag.</returns>
    /// <seealso cref="GetIdTitle"/>
    public static string Select(
        string name, IEnumerable collection, GetIdTitle getIdTitle, object selectedValue, bool firstEmpty)
    {
      return Select(name, name, collection, getIdTitle, selectedValue, firstEmpty);
    }


    /// <summary>
    /// Creates a select list with the values in a collection.
    /// </summary>
    /// <param name="name">Name of the SELECT-tag</param>
    /// <param name="id">Id of the SELECT-tag</param>
    /// <param name="collection">collection used to generate options.</param>
    /// <param name="getIdTitle">delegate used to return id and title from objects.</param>
    /// <param name="selectedValue">value that should be marked as selected.</param>
    /// <param name="firstEmpty">First row should contain an empty value.</param>
    /// <returns>string containing a SELECT-tag.</returns>
    /// <seealso cref="GetIdTitle"/>
    /// <example>
    /// <code>
    /// // Class that is going to be used in a SELECT-tag.
    /// public class User
    /// {
    ///     private readonly string _realName;
    ///     private readonly int _id;
    ///     public User(int id, string realName)
    ///     {
    ///         _id = id;
    ///         _realName = realName;
    ///     }
    ///     public string RealName
    ///     {
    ///         get { return _realName; }
    ///     }
    /// 
    ///     public int Id
    ///     {
    ///         get { return _id; }
    ///     }
    /// }
    /// 
    /// // Using an inline delegate to generate the select list
    /// public void UserInlineDelegate()
    /// {
    ///     List&lt;User&gt; items = new List&lt;User&gt;();
    ///     items.Add(new User(1, "adam"));
    ///     items.Add(new User(2, "bertial"));
    ///     items.Add(new User(3, "david"));
    ///     string htmlSelect = Select("users", "users", items, delegate(object o, out object id, out object value)
    ///                                                         {
    ///                                                             User user = (User)o;
    ///                                                             id = user.Id;
    ///                                                             value = user.RealName;
    ///                                                         }, 2, true);
    /// }
    /// 
    /// // Using an method as delegate to generate the select list.
    /// public void UseExternalDelegate()
    /// {
    ///     List&lt;User&gt; items = new List&lt;User&gt;();
    ///     items.Add(new User(1, "adam"));
    ///     items.Add(new User(2, "bertial"));
    ///     items.Add(new User(3, "david"));
    ///     string htmlSelect = Select("users", "users", items, UserOptions, 1, true);
    /// }
    /// 
    /// // delegate returning id and title
    /// public static void UserOptions(object o, out object id, out object title)
    /// {
    ///     User user = (User)o;
    ///     id = user.Id;
    ///     value = user.RealName;
    /// }
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException"><c>name</c>, <c>id</c>, <c>collection</c> or <c>getIdTitle</c> is null.</exception>
    public static string Select(
        string name, string id, IEnumerable collection, GetIdTitle getIdTitle, object selectedValue, bool firstEmpty)
    {
      if (name == null)
        throw new ArgumentNullException("name");
      if (id == null)
        throw new ArgumentNullException("id");
      if (collection == null)
        throw new ArgumentNullException("collection");
      if (getIdTitle == null)
        throw new ArgumentNullException("getIdTitle");

      StringBuilder sb = new StringBuilder();
      sb.AppendLine("<select name=\"" + name + "\" id=\"" + id + "\">");
      Options(sb, collection, getIdTitle, selectedValue, firstEmpty);
      sb.AppendLine("</select>");
      return sb.ToString();
    }

    /// <summary>
    /// Creates a select list with the values in a collection.
    /// </summary>
    /// <param name="name">Name of the SELECT-tag</param>
    /// <param name="id">Id of the SELECT-tag</param>
    /// <param name="collection">collection used to generate options.</param>
    /// <param name="getIdTitle">delegate used to return id and title from objects.</param>
    /// <param name="selectedValue">value that should be marked as selected.</param>
    /// <param name="firstEmpty">First row should contain an empty value.</param>
    /// <param name="htmlAttributes">name, value collection of extra HTML attributes.</param>
    /// <returns>string containing a SELECT-tag.</returns>
    /// <seealso cref="GetIdTitle"/>
    /// <exception cref="ArgumentNullException"><c>name</c>, <c>id</c>, <c>collection</c> or <c>getIdTitle</c> is null.</exception>
    /// <exception cref="ArgumentException">Invalid HTML attribute list.</exception>
    public static string Select(
        string name, string id, IEnumerable collection, GetIdTitle getIdTitle, object selectedValue, bool firstEmpty,
        params string[] htmlAttributes)
    {
      if (name == null)
        throw new ArgumentNullException("name");
      if (id == null)
        throw new ArgumentNullException("id");
      if (collection == null)
        throw new ArgumentNullException("collection");
      if (getIdTitle == null)
        throw new ArgumentNullException("getIdTitle");
      if (htmlAttributes != null && (htmlAttributes.Length%2) != 0)
        throw new ArgumentException("Invalid HTML attribute list.");

      StringBuilder sb = new StringBuilder();
      sb.Append("<select name=\"" + name + "\" id=\"" + id + "\"");
      WebHelper.GenerateHtmlAttributes(sb, htmlAttributes);
      sb.AppendLine(">");
      Options(sb, collection, getIdTitle, selectedValue, firstEmpty);
      sb.AppendLine("</select>");
      return sb.ToString();
    }

    /// <summary>
    /// Generate a list of HTML options
    /// </summary>
    /// <param name="collection">collection used to generate options.</param>
    /// <param name="getIdTitle">delegate used to return id and title from objects.</param>
    /// <param name="selectedValue">value that should be marked as selected.</param>
    /// <param name="firstEmpty">First row should contain an empty value.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"><c>collection</c> or <c>getIdTitle</c> is null.</exception>
    public static string Options(IEnumerable collection, GetIdTitle getIdTitle, object selectedValue, bool firstEmpty)
    {
      if (collection == null)
        throw new ArgumentNullException("collection");
      if (getIdTitle == null)
        throw new ArgumentNullException("getIdTitle");
      StringBuilder sb = new StringBuilder();
      Options(sb, collection, getIdTitle, selectedValue, firstEmpty);
      return sb.ToString();
    }

    /// <exception cref="ArgumentNullException"><c>sb</c> is null.</exception>
    private static void Options(
        StringBuilder sb, IEnumerable collection, GetIdTitle getIdTitle, object selectedValue, bool firstEmpty)
    {
      if (sb == null)
        throw new ArgumentNullException("sb");
      if (collection == null)
        throw new ArgumentNullException("collection");
      if (getIdTitle == null)
        throw new ArgumentNullException("getIdTitle");

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

        if (value != null && selectedValue != null && value.Equals(selectedValue))
          sb.Append(" selected=\"selected\"");

        sb.Append(">");

        if (title == null)
          sb.AppendLine("&nbsp;</option>");
        else
        {
          sb.Append(title);
          sb.AppendLine("</option>");
        }
      }
    }

    /// <summary>
    /// Creates a check box.
    /// </summary>
    /// <param name="name">element name</param>
    /// <param name="value">element value</param>
    /// <param name="isChecked">determines if the check box is selected or not. This is done differently depending on the
    /// type of variable. A boolean simply triggers checked or not, all other types are compared with "value" to determine if 
    /// the box is checked or not. </param>
    /// <param name="htmlAttributes">a list with additional attributes (name, value, name, value).</param>
    /// <returns>a generated radio button</returns>
    public static string CheckBox(string name, object value, object isChecked, params string[] htmlAttributes)
    {
      return CheckBox(name, null, value, isChecked, htmlAttributes);
    }

    /// <summary>
    /// Creates a check box.
    /// </summary>
    /// <param name="name">element name</param>
    /// <param name="id">element id</param>
    /// <param name="value">element value</param>
    /// <param name="isChecked">determines if the check box is selected or not. This is done differently depending on the
    /// type of variable. A boolean simply triggers checked or not, all other types are compared with "value" to determine if 
    /// the box is checked or not. </param>
    /// <param name="htmlAttributes">a list with additional attributes (name, value, name, value).</param>
    /// <returns>a generated radio button</returns>
    /// <remarks>
    /// value in your business object. (check box will be selected if it matches the element value)
    /// </remarks>
    public static string CheckBox(
        string name, string id, object value, object isChecked, params string[] htmlAttributes)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("<input type=\"checkbox\" name=\"");
      sb.Append(name);
      sb.Append("\"");
      if (id != null)
      {
        sb.Append(" id=\"");
        sb.Append(id);
        sb.Append("\"");
      }

      sb.Append(" value=\"");
      sb.Append(value);
      sb.Append("\"");

      if (isChecked is bool && (bool) isChecked)
        sb.Append("checked=\"checked\"");
      else if (isChecked == value)
        sb.Append("checked=\"checked\"");

      WebHelper.GenerateHtmlAttributes(sb, htmlAttributes);
      sb.Append("/>");
      return sb.ToString();
    }

    /// <summary>
    /// Creates a check box.
    /// </summary>
    /// <param name="name">element name</param>
    /// <param name="id">element id</param>
    /// <param name="isChecked">determines if the check box is selected or not. This is done differently depending on the
    /// type of variable. A boolean simply triggers checked or not, all other types are compared with "value" to determine if 
    /// the box is checked or not. </param>
    /// <param name="htmlAttributes">a list with additional attributes (name, value, name, value).</param>
    /// <returns>a generated radio button</returns>
    /// <remarks>will set value to "1".</remarks>
    public static string CheckBox(string name, string id, object isChecked, params string[] htmlAttributes)
    {
      return CheckBox(name, id, 1, isChecked, htmlAttributes);
    }

    /// <summary>
    /// Creates a RadioButton.
    /// </summary>
    /// <param name="name">element name</param>
    /// <param name="value">element value</param>
    /// <param name="isSelected">determines if the radio button is selected or not. This is done differently depending on the
    /// type of variable. A boolean simply triggers checked or not, all other types are compared with "value" to determine if 
    /// the box is checked or not. </param>
    /// <param name="htmlAttributes">a list with additional attributes (name, value, name, value).</param>
    /// <returns>a generated radio button</returns>
    public static string RadioButton(string name, object value, object isSelected, params string[] htmlAttributes)
    {
      return RadioButton(name, null, value, isSelected, htmlAttributes);
    }

    /// <summary>
    /// Creates a RadioButton.
    /// </summary>
    /// <param name="name">element name</param>
    /// <param name="id">element id</param>
    /// <param name="value">element value</param>
    /// <param name="isSelected">determines if the radio button is selected or not. This is done differently depending on the
    /// type of variable. A boolean simply triggers checked or not, all other types are compared with "value" to determine if 
    /// the box is checked or not. </param>
    /// <param name="htmlAttributes">a list with additional attributes (name, value, name, value).</param>
    /// <returns>a generated radio button</returns>
    public static string RadioButton(
        string name, string id, object value, object isSelected, params string[] htmlAttributes)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("<input type=\"radio\" name=\"");
      sb.Append(name);
      sb.Append("\"");
      if (id != null)
      {
        sb.Append(" id=\"");
        sb.Append(id);
        sb.Append("\"");
      }
      sb.Append(" value=\"");
      sb.Append(value);
      sb.Append("\"");

      if (isSelected is bool && (bool) isSelected)
        sb.Append("checked=\"checked\"");
      else if (isSelected.Equals(value))
        sb.Append("checked=\"checked\"");

      WebHelper.GenerateHtmlAttributes(sb, htmlAttributes);
      sb.Append("/>");
      return sb.ToString();
    }

    /// <summary>
    /// form close tag
    /// </summary>
    /// <returns></returns>
    public static string End()
    {
      return "</form>";
    }
  }
}