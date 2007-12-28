using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Visuals
{
  public class ResourceDictionary : IDictionary
  {
    Dictionary<string, object> _dictionary;
    public ResourceDictionary()
    {
      _dictionary = new Dictionary<string, object>();
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
      get {
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
