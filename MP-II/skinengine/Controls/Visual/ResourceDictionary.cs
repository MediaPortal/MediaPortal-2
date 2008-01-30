using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Visuals
{
  public class ResourceDictionary : IDictionary
  {
    Property _sourceProperty;
    Dictionary<string, object> _dictionary;
    public ResourceDictionary()
    {
      _sourceProperty = new Property("");
      _dictionary = new Dictionary<string, object>();
    }

    public string Source
    {
      get
      {
        return _sourceProperty.GetValue() as string;
      }
      set
      {
        _sourceProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the source property.
    /// </summary>
    /// <value>The source property.</value>
    public Property SourceProperty
    {
      get
      {
        return _sourceProperty;
      }
      set
      {
        _sourceProperty = value;
      }
    }

    public void Merge(ResourceDictionary dict)
    {
      IDictionaryEnumerator enumer = dict.GetEnumerator();
      while (enumer.MoveNext())
      {
        _dictionary[(string)enumer.Key] = enumer.Value;
      }
    }

    #region IDictionary Members

    public void Add(object key, object value)
    {
      _dictionary[(string)key] = value;
    }

    public void Clear()
    {
      _dictionary.Clear();
    }

    public bool Contains(object key)
    {
      return _dictionary.ContainsKey((string)key);
    }

    public IDictionaryEnumerator GetEnumerator()
    {
      return _dictionary.GetEnumerator();
    }

    public bool IsFixedSize
    {
      get { return false; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    public ICollection Keys
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public void Remove(object key)
    {
      _dictionary.Remove((string)key);
    }

    public ICollection Values
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public object this[object key]
    {
      get
      {
        return _dictionary[(string)key];
      }
      set
      {
        _dictionary[(string)key] = value;
      }
    }

    #endregion

    #region ICollection Members

    public void CopyTo(Array array, int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int Count
    {
      get
      {
        return _dictionary.Count;
      }
    }

    public bool IsSynchronized
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public object SyncRoot
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _dictionary.GetEnumerator();
    }

    #endregion
  }
}
