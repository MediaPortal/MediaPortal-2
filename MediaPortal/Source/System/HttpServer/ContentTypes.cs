using System.Collections;

namespace HttpServer
{
  /// <summary>
  /// Lists content type mime types.
  /// </summary>
  public class ContentType
  {
    /// <summary>
    /// text/plain
    /// </summary>
    public const string Text = "text/plain";

    /// <summary>
    /// text/haml
    /// </summary>
    public const string Html = "text/html";

    /// <summary>
    /// content type for javascript documents = application/javascript
    /// </summary>
    /// <remarks>
    /// <para>
    /// RFC 4329 states that text/javascript have been superseeded by
    /// application/javascript. You might still want to check browser versions
    /// since older ones do not support application/javascript.
    /// </para>
    /// <para>Browser support: http://krijnhoetmer.nl/stuff/javascript/mime-types/</para>
    /// </remarks>
    public const string Javascript = "application/javascript";

    /// <summary>
    /// text/xml
    /// </summary>
    public const string Xml = "text/xml";
  }

  /// <summary>
  /// A list of content types
  /// </summary>
  public class ContentTypes : IEnumerable
  {
    private readonly string[] _contentTypes;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="types">Semicolon separated content types.</param>
    public ContentTypes(string types)
    {
      if (types == null)
        _contentTypes = new string[] {ContentType.Html};
      else
        _contentTypes = types.Split(';');
    }

    /// <summary>
    /// Get this first content type.
    /// </summary>
    public string First
    {
      get { return _contentTypes.Length == 0 ? string.Empty : _contentTypes[0]; }
    }

    /// <summary>
    /// Fetch a content type
    /// </summary>
    /// <param name="type">Part of type ("xml" would return "application/xml")</param>
    /// <returns></returns>
    /// <remarks>All content types are in lower case.</remarks>
    public string this[string type]
    {
      get
      {
        foreach (string contentType in _contentTypes)
        {
          if (contentType.Contains(type))
            return contentType;
        }

        return string.Empty;
      }
    }

    #region IEnumerable Members

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator GetEnumerator()
    {
      return _contentTypes.GetEnumerator();
    }

    #endregion

    /// <summary>
    /// Searches for the specified type
    /// </summary>
    /// <param name="type">Can also be a part of a type (searching for "xml" would return true for "application/xml").</param>
    /// <returns>true if type was found.</returns>
    public bool Contains(string type)
    {
      foreach (string contentType in _contentTypes)
      {
        if (contentType.Contains(type))
          return true;
      }

      return false;
    }
  }
}