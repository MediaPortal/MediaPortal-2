using System;
using System.Collections;
using System.Collections.Generic;

namespace HttpServer
{
  /// <summary>
  /// Cookies that should be set.
  /// </summary>
  public sealed class ResponseCookies : IEnumerable<ResponseCookie>
  {
    private readonly IDictionary<string, ResponseCookie> _items = new Dictionary<string, ResponseCookie>();

    /// <summary>
    /// Adds a cookie in the collection.
    /// </summary>
    /// <param name="cookie">cookie to add</param>
    /// <exception cref="ArgumentNullException">cookie is null</exception>
    public void Add(ResponseCookie cookie)
    {
      // Verifies the parameter
      if (cookie == null)
        throw new ArgumentNullException("cookie");
      if (cookie.Name == null || cookie.Name.Trim() == string.Empty)
        throw new ArgumentException("Name must be specified.");
      if (cookie.Value == null || cookie.Value.Trim() == string.Empty)
        throw new ArgumentException("Content must be specified.");

      if (_items.ContainsKey(cookie.Name))
        _items[cookie.Name] = cookie;
      else _items.Add(cookie.Name, cookie);
    }

    /// <summary>
    /// Copy a request cookie
    /// </summary>
    /// <param name="cookie"></param>
    /// <param name="expires">When the cookie should expire</param>
    public void Add(RequestCookie cookie, DateTime expires)
    {
      Add(new ResponseCookie(cookie, expires));
    }

    /// <summary>
    /// Gets the count of cookies in the collection.
    /// </summary>
    public int Count
    {
      get { return _items.Count; }
    }


    /// <summary>
    /// Gets the cookie of a given identifier (null if not existing).
    /// </summary>
    public ResponseCookie this[string id]
    {
      get
      {
        if (_items.ContainsKey(id))
          return _items[id];
        else
          return null;
      }
      set
      {
        if (_items.ContainsKey(id))
          _items[id] = value;
        else
          Add(value);
      }
    }

    /// <summary>
    /// Gets a collection enumerator on the cookie list.
    /// </summary>
    /// <returns>collection enumerator</returns>
    public IEnumerator GetEnumerator()
    {
      return _items.Values.GetEnumerator();
    }


    /// <summary>
    /// Remove all cookies
    /// </summary>
    public void Clear()
    {
      _items.Clear();
    }

    #region IEnumerable<ResponseCookie> Members

    ///<summary>
    ///Returns an enumerator that iterates through the collection.
    ///</summary>
    ///
    ///<returns>
    ///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
    ///</returns>
    ///<filterpriority>1</filterpriority>
    IEnumerator<ResponseCookie> IEnumerable<ResponseCookie>.GetEnumerator()
    {
      return _items.Values.GetEnumerator();
    }

    #endregion
  }
}