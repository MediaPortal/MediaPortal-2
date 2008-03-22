#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Presentation.Properties;

namespace Presentation.SkinEngine.Controls.Visuals
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
