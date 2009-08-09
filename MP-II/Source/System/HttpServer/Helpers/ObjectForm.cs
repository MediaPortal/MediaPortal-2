using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace HttpServer.Helpers
{
  /// <summary>
  /// The object form class takes an object and creates form items for it.
  /// </summary>
  public class ObjectForm
  {
    private readonly object _object;
    private readonly string _action;
    private readonly string _name = string.Empty;
    private readonly string _method = "post";

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectForm"/> class.
    /// </summary>
    /// <param name="method"></param>
    /// <param name="name">form name *and* id.</param>
    /// <param name="action">action to do when form is posted.</param>
    /// <param name="obj"></param>
    public ObjectForm(string action, string name, object obj, string method)
        : this(name, action, obj)
    {
      if (string.IsNullOrEmpty(method))
        throw new ArgumentNullException("method");

      _method = method.ToLower();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectForm"/> class.
    /// </summary>
    /// <param name="name">form name *and* id.</param>
    /// <param name="action">action to do when form is posted.</param>
    /// <param name="obj">object to get values from</param>
    public ObjectForm(string action, string name, object obj)
        : this(action, obj)
    {
      if (string.IsNullOrEmpty(name))
        throw new ArgumentNullException("name");

      _name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectForm"/> class.
    /// </summary>
    /// <param name="action">form action.</param>
    /// <param name="obj">object to get values from.</param>
    public ObjectForm(string action, object obj)
    {
      if (string.IsNullOrEmpty(action))
        throw new ArgumentNullException("action");
      if (obj == null)
        throw new ArgumentNullException("obj");

      _object = obj;
      _action = action;
    }

    /// <summary>
    /// write out the FORM-tag.
    /// </summary>
    /// <returns>generated html code</returns>
    public string Begin()
    {
      return Begin(false);
    }

    /// <summary>
    /// Writeout the form tag
    /// </summary>
    /// <param name="isAjax">form should be posted through ajax.</param>
    /// <returns>generated html code</returns>
    public string Begin(bool isAjax)
    {
      string ajaxCode;
      if (isAjax)
        ajaxCode = "onsubmit=\"" + WebHelper.JSImplementation.AjaxFormOnSubmit() + "\"";
      else
        ajaxCode = string.Empty;

      if (!string.IsNullOrEmpty(_name))
        return string.Format(
            "<form method=\"{0}\" id=\"{1}\" name=\"{1}\" {3} action=\"{2}\">", _method, _name, _action, ajaxCode);
      else
        return string.Format("<form method=\"{0}\" action=\"{1}\" {2}>", _method, _action, ajaxCode);
    }


    /// <summary>
    /// Generates a text box.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="options"></param>
    /// <returns>generated html code</returns>
    public string Tb(string propertyName, params object[] options)
    {
      if (options.Length%2 != 0)
        throw new ArgumentException("Options must consist of string, object, ... as key/value couple(s).");

      if (options.Length == 0)
        return string.Format(
            "<input type=\"text\" name=\"{0}[{1}]\" id=\"{0}_{1}\" value=\"{2}\" />", _name, propertyName,
            GetValue(propertyName));

      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < options.Length; i += 2)
      {
        sb.Append(options[i]);
        sb.Append("=\"");
        sb.Append(options[i + 1].ToString());
        sb.Append("\"");
      }

      return string.Format(
          "<input type=\"text\" name=\"{0}[{1}]\" id=\"{0}_{1}\" {3} value=\"{2}\" />", _name, propertyName,
          GetValue(propertyName), sb);
    }

    /// <summary>
    /// password box
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="options"></param>
    /// <returns>generated html code</returns>
    public string Pb(string propertyName, params object[] options)
    {
      return string.Format("<input type=\"password\" name=\"{0}[{1}]\" id=\"{0}_{1}\" />", _name, propertyName);
    }

    /// <summary>
    /// Hiddens the specified property name.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="options">The options.</param>
    /// <returns>generated html code</returns>
    public string Hidden(string propertyName, params object[] options)
    {
      return string.Format(
          "<input type=\"hidden\" name=\"{0}[{1}]\" id=\"{0}_{1}\" value=\"{2}\" />", _name, propertyName,
          GetValue(propertyName));
    }

    /// <summary>
    /// Labels the specified property name.
    /// </summary>
    /// <param name="propertyName">property in object.</param>
    /// <param name="label">caption</param>
    /// <returns>generated html code</returns>
    public string Label(string propertyName, string label)
    {
      return string.Format("<label for=\"{0}_{1}\">{2}</label>", _name, propertyName, label);
    }

    /// <summary>
    /// Generate a checkbox
    /// </summary>
    /// <param name="propertyName">property in object</param>
    /// <param name="value">checkbox value</param>
    /// <param name="options">additional html attributes.</param>
    /// <returns>generated html code</returns>
    public string Cb(string propertyName, string value, params object[] options)
    {
      string isChecked = string.Empty;

      object o = GetValue(propertyName);
      if (o.GetType() == typeof(bool) && (bool) o)
        isChecked = "checked=\"checked\"";
      else
      {
        int i;
        int.TryParse(o.ToString(), out i);
        if (i != 0)
          isChecked = "checked=\"checked\"";
      }

      return
          string.Format(
              "<input type=\"hidden\" name=\"{0}\" id=\"{0}_{1}\" value=\"0\" /><input type=\"checkbox\" name=\"{0}[{1}]\" id=\"{0}_{1}\" value=\"{2}\" {3} />",
              _name, propertyName, value, isChecked);
    }

    /// <summary>
    /// Write a html select tag
    /// </summary>
    /// <param name="propertyName">object property.</param>
    /// <param name="idColumn">id column</param>
    /// <param name="titleColumn">The title column.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public string Select(string propertyName, string idColumn, string titleColumn, params object[] options)
    {
      object o = _object.GetType().GetProperty(propertyName).GetValue(_object, null);
      return Select(propertyName, o as IEnumerable, idColumn, titleColumn, options);
    }

    /// <summary>
    /// Selects the specified property name.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="items">The items.</param>
    /// <param name="idColumn">The id column.</param>
    /// <param name="titleColumn">The title column.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public string Select(
        string propertyName, IEnumerable items, string idColumn, string titleColumn, params object[] options)
    {
      //object o = _object.GetType().GetProperty(propertyName).GetValue(_object, null);

      StringBuilder sb = new StringBuilder();
      sb.AppendFormat("<select name=\"{0}[{1}]\">{2}", _name, propertyName, Environment.NewLine);

      if (items != null)
        foreach (object o1 in items)
        {
          sb.AppendFormat(
              "<option value=\"{0}\">{1}</option>{2}",
              o1.GetType().GetProperty(idColumn).GetValue(o1, null),
              o1.GetType().GetProperty(titleColumn).GetValue(o1, null), Environment.NewLine);
        }
      sb.AppendLine("</select>");
      return sb.ToString();
    }

    /// <summary>
    /// Write a submit tag.
    /// </summary>
    /// <param name="value">button caption</param>
    /// <returns>html submit tag</returns>
    public string Submit(string value)
    {
      return string.Format("<input type=\"submit\" value=\"{0}\" />", value);
    }

    /// <summary>
    /// html end form tag
    /// </summary>
    /// <returns>html</returns>
    public string End()
    {
      return "</form>";
    }

    private string GetValue(string propertyName)
    {
      Type type = _object.GetType();
      PropertyInfo pi = type.GetProperty(propertyName);
      if (pi == null) throw new ArgumentException("Property " + propertyName + " not found for type " + type);
      object o = pi.GetValue(_object, null);
      if (o == null)
        return string.Empty;
      else
        return o.ToString();
    }
  }
}