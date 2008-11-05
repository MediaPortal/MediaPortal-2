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
using System.Data;
using MediaPortal.Database;
using MediaPortal.Database.Provider;

namespace Components.Database
{
  public class DbItem : IDbItem
  {
    #region variables

    private Dictionary<string, IDbAttribute> _attributes;
    private IDatabase _database;

    #endregion

    #region IDbItem Members

    /// <summary>
    /// Initializes a new instance of the <see cref="DbItem"/> class.
    /// </summary>
    /// <param name="database">The database.</param>
    public DbItem(IDatabase database)
    {
      _attributes = new Dictionary<string, IDbAttribute>();
      _database = database;
      GetDefaultAttributes();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbItem"/> class.
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="reader">The reader.</param>
    public DbItem(IDatabase database, IDataReader reader)
    {
      _database = database;
      GetDefaultAttributes();
      for (int i = 0; i < reader.FieldCount; ++i)
      {
        object val;
        string name = reader.GetName(i);
        string type = reader.GetDataTypeName(i);
        if (type == "DATE")
        {
          val = reader.GetDateTime(i);
        }
        else
        {
          val = reader[i];
        }
        if (val != null && val != DBNull.Value)
        {
          // In case of a multi value field, remove the pipe symbol
          if (_attributes[name].IsList)
            _attributes[name].Value = val.ToString().Trim(new char[] { '|' });
          else
            _attributes[name].Value = val;
          _attributes[name].IsChanged = false;
        }
      }
    }

    /// <summary>
    /// Gets the default attributes.
    /// </summary>
    private void GetDefaultAttributes()
    {
      _attributes = new Dictionary<string, IDbAttribute>();
      IList<IDbAttribute> list = _database.Attributes;
      foreach (IDbAttribute attr in list)
      {
        _attributes[attr.Name] = (IDbAttribute) attr.Clone();
      }
    }

    /// <summary>
    /// Gets the attributes for this item
    /// </summary>
    /// <value>The attributes.</value>
    public IDictionary<string, IDbAttribute> Attributes
    {
      get { return _attributes; }
    }

    /// <summary>
    /// save item in the database
    /// </summary>
    public void Save()
    {
      if (Changed)
      {
        using (IDatabaseConnection connect = _database.Builder.CreateConnection())
        {
          using (IDatabaseCommand cmd = _database.Builder.CreateCommand())
          {
            cmd.Connection = connect;
            cmd.Save(this);
          }
        }
      }
    }

    /// <summary>
    /// delete the item
    /// </summary>
    public void Delete()
    {
      using (IDatabaseConnection connect = _database.Builder.CreateConnection())
      {
        using (IDatabaseCommand cmd = _database.Builder.CreateCommand())
        {
          cmd.Connection = connect;
          cmd.Delete(this);

          //reset all attributes to their default values
          GetDefaultAttributes();
        }
      }
    }

    /// <summary>
    /// Gets a value indicating whether this <see cref="DbItem"/> is changed.
    /// </summary>
    /// <value><c>true</c> if changed; otherwise, <c>false</c>.</value>
    public bool Changed
    {
      get
      {
        IEnumerator<KeyValuePair<string, IDbAttribute>> enumer = Attributes.GetEnumerator();
        while (enumer.MoveNext())
          if (enumer.Current.Value.IsChanged)
            return true;
        return false;
      }
    }

    /// <summary>
    /// Gets the database.
    /// </summary>
    /// <value>The database.</value>
    public IDatabase Database
    {
      get { return _database; }
    }


    /// <summary>
    /// Gets or sets the <see cref="System.Object"/> with the specified key.
    /// </summary>
    /// <value></value>
    public object this[string key]
    {
      get { return _attributes[key].Value; }
      set { _attributes[key].Value = value; }
    }

    #endregion
  }
}
