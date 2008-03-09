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
using System.Text;
using MediaPortal.Database;

namespace Database
{
  public class DbMultiFieldAttribute : IDbAttribute, ICloneable
  {
    public class FieldInfo
    {
      public string Value;
      public int Id;
      public FieldInfo(string text)
      {
        Id = -1;
        Value = text;
      }
    };
    bool _changed;
    string _tableName;
    string _linkTable;
    string _fieldName;
    List<FieldInfo> _values = new List<FieldInfo>();
    bool _fetched;
    IDbItem _item;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbListAttribute"/> class.
    /// </summary>
    /// <param name="att">The att.</param>
    public DbMultiFieldAttribute(DbMultiFieldAttribute att)
    {
      _changed = att._changed;
      _fieldName = att._fieldName;
      _tableName = att._tableName;
      _linkTable = att._linkTable;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbAttribute"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    public DbMultiFieldAttribute(string fieldName, string tablename, string linkTable)
    {
      _fieldName = fieldName;
      _tableName = tablename;
      _linkTable = linkTable;
      _changed = false;
    }

    public IDbItem Item
    {
      get
      {
        return _item;
      }
      set
      {
        _item=value;
      }
    }
    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get
      {
        return _fieldName;
      }
    }
    public string TableName
    {
      get
      {
        return _tableName;
      }
    }
    public string LinkTableName
    {
      get
      {
        return _linkTable;
      }
    }

    /// <summary>
    /// Gets the attribute type.
    /// </summary>
    /// <value>The type.</value>
    public Type Type
    {
      get
      {
        return this.GetType();
      }
    }

    /// <summary>
    /// Gets the field-size.
    /// </summary>
    /// <value>The size.</value>
    public int Size
    {
      get
      {
        return 0;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is changed.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is changed; otherwise, <c>false</c>.
    /// </value>
    public bool IsChanged
    {
      get
      {
        return _changed;
      }
      set
      {
        _changed = value;
      }
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <value>The value.</value>
    public object Value
    {
      get
      {
        if (_fetched == false)
          Fetch();
        return null;
      }
      set
      {
        string text = (string)value;
        _values.Clear();
        string[] parts = text.Split(new char[] { '|' });
        for (int i = 0; i < parts.Length; ++i)
          _values.Add(new FieldInfo(parts[i]));
        _changed = true;
      }
    }

    public List<FieldInfo> Values
    {
      get
      {
        if (_fetched == false) 
          Fetch();
        return _values;
      }
      set
      {
        _values = value;
      }
    }

    #region ICloneable Members

    public object Clone()
    {
      return new DbMultiFieldAttribute(this);
    }

    void Fetch()
    {
      _fetched = true;

    }
    #endregion
  }
}

