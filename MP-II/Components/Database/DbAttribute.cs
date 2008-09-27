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
using System.Collections.Generic;
using MediaPortal.Database;

namespace Components.Database
{
  public class DbAttribute : IDbAttribute, ICloneable
  {
    #region variables

    private bool _changed;
    private string _name;
    private Type _type;
    private int _size;
    private object _value;
    private bool _isList;
    private IList<string> _values;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="DbAttribute"/> class.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="type">The attribute type.</param>
    public DbAttribute(string name, Type type)
    {
      _name = name;
      _type = type;
      _changed = false;
      if (type == typeof (List<string>))
      {
        _type = typeof (string);
        _isList = true;
        _values = new List<string>();
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbAttribute"/> class.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="type">The attribute type.</param>
    /// <param name="size">The attribute size.</param>
    public DbAttribute(string name, Type type, int size)
    {
      _name = name;
      _type = type;
      _size = size;
      _changed = false;
      if (type == typeof (List<string>))
      {
        _type = typeof (string);
        _isList = true;
        _values = new List<string>();
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbAttribute"/> class.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="type">The attribute type.</param>
    /// <param name="value">The attribute value.</param>
    public DbAttribute(string name, Type type, object value)
    {
      _name = name;
      _type = type;
      if (type == typeof (List<string>))
      {
        _type = typeof (string);
        _isList = true;
        _values = new List<string>();
      }
      Value = value;
      _changed = false;
    }

    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets the attribute type.
    /// </summary>
    /// <value>The type.</value>
    public Type Type
    {
      get { return _type; }
    }

    /// <summary>
    /// Gets the attribute field-size.
    /// </summary>
    /// <value>The size.</value>
    public int Size
    {
      get { return _size; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is changed.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is changed; otherwise, <c>false</c>.
    /// </value>
    public bool IsChanged
    {
      get { return _changed; }
      set { _changed = value; }
    }

    /// <summary>
    /// Gets the attribute value.
    /// </summary>
    /// <value>The attribute value.</value>
    public object Value
    {
      get
      {
        if (IsList)
        {
          string line = "";
          foreach (string key in _values)
          {
            line += key + "|";
          }
          if (line.EndsWith("|"))
          {
            line = line.Substring(0, line.Length - 1);
          }
          _value = line;
        }
        return _value;
      }
      set
      {
        if (_type == typeof (DateTime))
        {
          if (value == null)
          {
            _value = DateTime.MinValue;
          }
          else if (value.GetType() == typeof (string))
          {
            _value = DateTime.Parse(value.ToString());
          }
          else
          {
            _value = value;
          }
        }
        else
        {
          _value = value;
        }
        _changed = true;
        if (IsList)
        {
          _values.Clear();
          string[] parts = _value.ToString().Split(new char[] {'|'});
          for (int i = 0; i < parts.Length; ++i)
          {
            _values.Add(parts[i]);
          }
          return;
        }
      }
    }

    /// <summary>
    /// Gets a value indicating whether this attribute contains a list of items.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this attribute contains a list of items; otherwise, <c>false</c>.
    /// </value>
    public bool IsList
    {
      get { return _isList; }
    }

    /// <summary>
    /// if IsList is true, then this property can be used to get/set the values
    /// </summary>
    /// <value>The values.</value>
    public IList<string> Values
    {
      get { return _values; }
      set { _values = value; }
    }

    #region ICloneable Members

    /// <summary>
    /// Creates a new object that is a copy of the current instance.
    /// </summary>
    /// <returns>
    /// A new object that is a copy of this instance.
    /// </returns>
    public object Clone()
    {
      DbAttribute att = new DbAttribute(Name, Type, Value);
      att._isList = _isList;
      att._changed = _changed;
      att._values = new List<string>();
      return att;
    }

    #endregion
  }
}
