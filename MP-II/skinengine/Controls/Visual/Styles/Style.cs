using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Visuals.Styles
{
  public class Style : ICloneable, IList
  {
    SetterCollection _setters;
    Property _keyProperty;

    public Style()
    {
      Init();
    }

    public Style(Style s)
    {
      Init();
      Key = s.Key;
      foreach (Setter set in s._setters)
      {
        _setters.Add((Setter)set.Clone());
      }
    }

    public object Clone()
    {
      return new Style(this);
    }
    void Init()
    {
      _setters = new SetterCollection();
      _keyProperty = new Property("");
    }

    /// <summary>
    /// Gets or sets the key property.
    /// </summary>
    /// <value>The key property.</value>
    public Property KeyProperty
    {
      get
      {
        return _keyProperty;
      }
      set
      {
        _keyProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    public string Key
    {
      get
      {
        return _keyProperty.GetValue() as string;
      }
      set
      {
        _keyProperty.SetValue(value);
      }
    }

    #region IList Members

    public int Add(object value)
    {
      _setters.Add((Setter)value);
      return _setters.Count;
    }

    public void Clear()
    {
      _setters.Clear();
    }

    public bool Contains(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int IndexOf(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Insert(int index, object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public bool IsFixedSize
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsReadOnly
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public void Remove(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void RemoveAt(int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public object this[int index]
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
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
        return _setters.Count;
      }
    }

    public bool IsSynchronized
    {
      get
      {
        return true;
      }
    }

    public object SyncRoot
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion

    public void Set(UIElement element)
    {
      foreach (Setter setter in _setters)
      {
        Set(element, setter);
      }
    }

    void Set(UIElement element, Setter setter)
    {
      Type t = element.GetType();
      PropertyInfo pinfo = t.GetProperty(setter.Property + "Property");
      if (pinfo == null) return;
      MethodInfo minfo = pinfo.GetGetMethod();
      Property property = minfo.Invoke(element, null) as Property;
      ICloneable clone = setter.Value as ICloneable;
      if (clone != null)
      {
        property.SetValue(clone.Clone());
      }
    }
  }
}
