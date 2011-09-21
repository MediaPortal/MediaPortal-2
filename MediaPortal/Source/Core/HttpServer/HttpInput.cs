using System;
using System.Collections;
using System.Collections.Generic;

namespace HttpServer
{
  /// <summary>
  /// Contains some kind of input from the browser/client.
  /// can be QueryString, form data or any other request body content.
  /// </summary>
  public class HttpInput : IHttpInput
  {
    /// <summary> Representation of a non-initialized class instance </summary>
    public static readonly HttpInput Empty = new HttpInput("Empty", true);

    private readonly IDictionary<string, HttpInputItem> _items = new Dictionary<string, HttpInputItem>();
    private string _name;

    /// <summary> Variable telling the class that it is non-initialized <see cref="Empty"/> </summary>
    protected readonly bool _ignoreChanges;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpInput"/> class.
    /// </summary>
    /// <param name="name">form name.</param>
    public HttpInput(string name)
    {
      Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpInput"/> class.
    /// </summary>
    /// <param name="name">form name.</param>
    /// <param name="ignoreChanges">if set to <c>true</c> all changes will be ignored. </param>
    /// <remarks>this constructor should only be used by Empty</remarks>
    protected HttpInput(string name, bool ignoreChanges)
    {
      _name = name;
      _ignoreChanges = ignoreChanges;
    }

    /// <summary>Creates a deep copy of the HttpInput class</summary>
    /// <param name="input">The object to copy</param>
    /// <remarks>The function makes a deep copy of quite a lot which can be slow</remarks>
    protected HttpInput(HttpInput input)
    {
      foreach (HttpInputItem item in input)
        _items.Add(item.Name, new HttpInputItem(item));

      _name = input._name;
      _ignoreChanges = input._ignoreChanges;
    }

    /// <summary>
    /// Form name as lower case
    /// </summary>
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Add a new element. Form array elements are parsed
    /// and added in a correct hierarchy.
    /// </summary>
    /// <param name="name">Name is converted to lower case.</param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentNullException"><c>name</c> is null.</exception>
    /// <exception cref="InvalidOperationException">Cannot add stuff to <see cref="HttpInput.Empty"/>.</exception>
    public void Add(string name, string value)
    {
      if (name == null)
        throw new ArgumentNullException("name");
      if (_ignoreChanges)
        throw new InvalidOperationException("Cannot add stuff to HttpInput.Empty.");

      // Check if it's a sub item.
      // we can have multiple levels of sub items as in user[extension[id]] => user -> extension -> id
      int pos = name.IndexOf('[');
      if (pos != -1)
      {
        string name1 = name.Substring(0, pos);
        string name2 = ExtractOne(name);
        if (!_items.ContainsKey(name1))
          _items.Add(name1, new HttpInputItem(name1, null));
        _items[name1].Add(name2, value);
      }
      else
      {
        if (_items.ContainsKey(name))
          _items[name].Add(value);
        else
          _items.Add(name, new HttpInputItem(name, value));
      }
    }

    /// <summary>
    /// Get a form item.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>Returns <see cref="HttpInputItem.Empty"/> if item was not found.</returns>
    public HttpInputItem this[string name]
    {
      get { return _items.ContainsKey(name) ? _items[name] : HttpInputItem.Empty; }
    }

    /// <summary>
    /// Returns true if the class contains a <see cref="HttpInput"/> with the corresponding name.
    /// </summary>
    /// <param name="name">The field/query string name</param>
    /// <returns>True if the value exists</returns>
    public bool Contains(string name)
    {
      return _items.ContainsKey(name) && _items[name].Value != null;
    }

    /// <summary>
    /// Parses an item and returns it.
    /// This function is primarily used to parse array items as in user[name].
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static HttpInputItem ParseItem(string name, string value)
    {
      HttpInputItem item;

      // Check if it's a sub item.
      // we can have multiple levels of sub items as in user[extension[id]]] => user -> extension -> id
      int pos = name.IndexOf('[');
      if (pos != -1)
      {
        string name1 = name.Substring(0, pos);
        string name2 = ExtractOne(name);
        item = new HttpInputItem(name1, null);
        item.Add(name2, value);
      }
      else
        item = new HttpInputItem(name, value);

      return item;
    }

    /// <summary> Outputs the instance representing all its values joined together </summary>
    /// <returns></returns>
    public override string ToString()
    {
      string temp = string.Empty;
      foreach (KeyValuePair<string, HttpInputItem> item in _items)
        temp += item.Value.ToString(Name);
      return temp;
    }

    /// <summary>Returns all items as an unescaped query string.</summary>
    /// <returns></returns>
    public string ToString(bool asQueryString)
    {
      if (!asQueryString)
        return ToString();

      string temp = string.Empty;
      foreach (KeyValuePair<string, HttpInputItem> item in _items)
        temp += item.Value.ToString(null, true) + '&';

      return temp.Length > 0 ? temp.Substring(0, temp.Length - 1) : string.Empty;
    }

    /// <summary>
    /// Extracts one parameter from an array
    /// </summary>
    /// <param name="value">Containing the string array</param>
    /// <returns>All but the first value</returns>
    /// <example>
    /// string test1 = ExtractOne("system[user][extension][id]");
    /// string test2 = ExtractOne(test1);
    /// string test3 = ExtractOne(test2);
    /// // test1 = user[extension][id]
    /// // test2 = extension[id]
    /// // test3 = id
    /// </example>
    public static string ExtractOne(string value)
    {
      int pos = value.IndexOf('[');
      if (pos != -1)
      {
        ++pos;
        int gotMore = value.IndexOf('[', pos + 1);
        if (gotMore != -1)
          value = value.Substring(pos, gotMore - pos - 1) + value.Substring(gotMore);
        else
          value = value.Substring(pos, value.Length - pos - 1);
      }
      return value;
    }

    /// <summary>Resets all data contained by class</summary>
    public virtual void Clear()
    {
      _name = string.Empty;
      _items.Clear();
    }

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
  }

  /// <summary>
  /// Base class for request data containers
  /// </summary>
  public interface IHttpInput : IEnumerable<HttpInputItem>
  {
    /// <summary>
    /// Adds a parameter mapped to the presented name
    /// </summary>
    /// <param name="name">The name to map the parameter to</param>
    /// <param name="value">The parameter value</param>
    void Add(string name, string value);

    /// <summary>
    /// Returns a request parameter
    /// </summary>
    /// <param name="name">The name associated with the parameter</param>
    /// <returns></returns>
    HttpInputItem this[string name] { get; }

    /// <summary>
    /// Returns true if the container contains the requested parameter
    /// </summary>
    /// <param name="name">Parameter id</param>
    /// <returns>True if parameter exists</returns>
    bool Contains(string name);
  }
}