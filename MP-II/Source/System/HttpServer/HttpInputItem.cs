using System;
using System.Collections;
using System.Collections.Generic;

namespace HttpServer
{
  /// <summary>
  /// represents a HTTP input item. Each item can have multiple sub items, a sub item
  /// is made in a HTML form by using square brackets
  /// </summary>
  /// <example>
  ///   // <input type="text" name="user[FirstName]" value="jonas" /> becomes:
  ///   Console.WriteLine("Value: {0}", form["user"]["FirstName"].Value);
  /// </example>
  /// <remarks>
  /// All names in a form SHOULD be in lowercase.
  /// </remarks>
  public class HttpInputItem : IHttpInput
  {
    /// <summary> Representation of a non-initialized <see cref="HttpInputItem"/>.</summary>
    public static readonly HttpInputItem Empty = new HttpInputItem(string.Empty, true);

    private readonly IDictionary<string, HttpInputItem> _items = new Dictionary<string, HttpInputItem>();
    private readonly List<string> _values = new List<string>();
    private string _name;
    private readonly bool _ignoreChanges;

    /// <summary>
    /// Initializes an input item setting its name/identifier and value
    /// </summary>
    /// <param name="name">Parameter name/id</param>
    /// <param name="value">Parameter value</param>
    public HttpInputItem(string name, string value)
    {
      Name = name;
      Add(value);
    }

    private HttpInputItem(string name, bool ignore)
    {
      Name = name;
      _ignoreChanges = ignore;
    }

    /// <summary>Creates a deep copy of the item specified</summary>
    /// <param name="item">The item to copy</param>
    /// <remarks>The function makes a deep copy of quite a lot which can be slow</remarks>
    public HttpInputItem(HttpInputItem item)
    {
      foreach (KeyValuePair<string, HttpInputItem> pair in item._items)
        _items.Add(pair.Key, pair.Value);

      foreach (string value in item._values)
        _values.Add(value);

      _ignoreChanges = item._ignoreChanges;
      _name = item.Name;
    }

    /// <summary>
    /// Number of values
    /// </summary>
    public int Count
    {
      get { return _values.Count; }
    }

    /// <summary>
    /// Get a sub item
    /// </summary>
    /// <param name="name">name in lower case.</param>
    /// <returns><see cref="HttpInputItem.Empty"/> if no item was found.</returns>
    public HttpInputItem this[string name]
    {
      get { return _items.ContainsKey(name) ? _items[name] : Empty; }
    }

    /// <summary>
    /// Name of item (in lower case).
    /// </summary>
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Returns the first value, or null if no value exist.
    /// </summary>
    public string Value
    {
      get { return _values.Count == 0 ? null : _values[0]; }
      set
      {
        if (_values.Count == 0)
          _values.Add(value);
        else
          _values[0] = value;
      }
    }

    /// <summary>
    /// Returns the last value, or null if no value exist.
    /// </summary>
    public string LastValue
    {
      get { return _values.Count == 0 ? null : _values[_values.Count - 1]; }
    }

    /// <summary>
    /// Returns the list with values.
    /// </summary>
    public IList<string> Values
    {
      get { return _values.AsReadOnly(); }
    }


    /// <summary>
    /// Add another value to this item
    /// </summary>
    /// <param name="value">Value to add.</param>
    /// <exception cref="InvalidOperationException">Cannot add stuff to <see cref="HttpInput.Empty"/>.</exception>
    public void Add(string value)
    {
      if (value == null)
        return;
      if (_ignoreChanges)
        throw new InvalidOperationException("Cannot add stuff to HttpInput.Empty.");

      _values.Add(value);
    }

    /// <summary>
    /// checks if a sub-item exists (and has a value).
    /// </summary>
    /// <param name="name">name in lower case</param>
    /// <returns>true if the sub-item exists and has a value; otherwise false.</returns>
    public bool Contains(string name)
    {
      return _items.ContainsKey(name) && _items[name].Value != null;
    }

    /// <summary> Returns a formatted representation of the instance with the values of all contained parameters </summary>
    public override string ToString()
    {
      return ToString(string.Empty);
    }

    /// <summary>
    /// Outputs the string in a formatted manner
    /// </summary>
    /// <param name="prefix">A prefix to append, used internally</param>
    /// <param name="asQuerySting">produce a query string</param>
    public string ToString(string prefix, bool asQuerySting)
    {
      string name;
      if (string.IsNullOrEmpty(prefix))
        name = Name;
      else
        name = prefix + "[" + Name + "]";

      if (asQuerySting)
      {
        string temp;
        if (_values.Count == 0 && _items.Count > 0)
          temp = string.Empty;
        else
          temp = name;

        if (_values.Count > 0)
        {
          temp += '=';
          foreach (string value in _values)
            temp += value + ',';
          temp = temp.Remove(temp.Length - 1, 1);
        }

        foreach (KeyValuePair<string, HttpInputItem> item in _items)
          temp += item.Value.ToString(name, true) + '&';

        return _items.Count > 0 ? temp.Substring(0, temp.Length - 1) : temp;
      }
      else
      {
        string temp = name;
        if (_values.Count > 0)
        {
          temp += " = ";
          foreach (string value in _values)
            temp += value + ", ";
          temp = temp.Remove(temp.Length - 2, 2);
        }
        temp += Environment.NewLine;

        foreach (KeyValuePair<string, HttpInputItem> item in _items)
          temp += item.Value.ToString(name, false);
        return temp;
      }
    }

    #region IHttpInput Members

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">name in lower case</param>
    /// <returns></returns>
    HttpInputItem IHttpInput.this[string name]
    {
      get { return _items.ContainsKey(name) ? _items[name] : Empty; }
    }

    /// <summary>
    /// Add a sub item.
    /// </summary>
    /// <param name="name">Can contain array formatting, the item is then parsed and added in multiple levels</param>
    /// <param name="value">Value to add.</param>
    /// <exception cref="ArgumentNullException">Argument is null.</exception>
    /// <exception cref="InvalidOperationException">Cannot add stuff to <see cref="HttpInput.Empty"/>.</exception>
    public void Add(string name, string value)
    {
      if (name == null && value != null)
        throw new ArgumentNullException("name");
      if (name == null)
        return;
      if (_ignoreChanges)
        throw new InvalidOperationException("Cannot add stuff to HttpInput.Empty.");

      int pos = name.IndexOf('[');
      if (pos != -1)
      {
        string name1 = name.Substring(0, pos);
        string name2 = HttpInput.ExtractOne(name);
        if (!_items.ContainsKey(name1))
          _items.Add(name1, new HttpInputItem(name1, null));
        _items[name1].Add(name2, value);
        /*
        HttpInputItem item = HttpInput.ParseItem(name, value);

        // Add the value to an existing sub item
        if (_items.ContainsKey(item.Name))
          _items[item.Name].Add(item.Value);
        else
          _items.Add(item.Name, item);
        */
      }
      else
      {
        if (_items.ContainsKey(name))
          _items[name].Add(value);
        else
          _items.Add(name, new HttpInputItem(name, value));
      }
    }

    #endregion

    ///<summary>
    ///Returns an enumerator that iterates through the collection.
    ///</summary>
    ///
    ///<returns>
    ///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
    ///</returns>
    ///<filterpriority>1</filterpriority>
    IEnumerator<HttpInputItem> IEnumerable<HttpInputItem>.GetEnumerator()
    {
      return _items.Values.GetEnumerator();
    }

    #region IEnumerable Members

    ///<summary>
    ///Returns an enumerator that iterates through a collection.
    ///</summary>
    ///
    ///<returns>
    ///An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
    ///</returns>
    ///<filterpriority>2</filterpriority>
    public IEnumerator GetEnumerator()
    {
      return _items.Values.GetEnumerator();
    }

    #endregion

    /// <summary>
    /// Outputs the string in a formatted manner
    /// </summary>
    /// <param name="prefix">A prefix to append, used internally</param>
    /// <returns></returns>
    public string ToString(string prefix)
    {
      return ToString(prefix, false);
    }
  }
}