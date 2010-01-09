#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Data;

namespace MediaPortal.Utilities.DB
{
  public class DBUtils
  {
    public static object ReadDBObject(IDataReader reader, int colIndex)
    {
      if (reader.IsDBNull(colIndex))
        return null;
      return reader.GetValue(colIndex);
    }

    public static T ReadDBValue<T>(IDataReader reader, int colIndex)
    {
      return (T) ReadDBValue(typeof(T), reader, colIndex);
    }

    public static object ReadDBValue(Type type, IDataReader reader, int colIndex)
    {
      if (reader.IsDBNull(colIndex))
        return null;
      if (type == typeof(string))
        return reader.GetString(colIndex);
      else if (type == typeof(DateTime))
        return reader.GetDateTime(colIndex);
      else if (type == typeof(Char))
        return reader.GetChar(colIndex);
      else if (type == typeof(Boolean))
        return reader.GetBoolean(colIndex);
      else if (type == typeof(Single))
        return reader.GetFloat(colIndex);
      else if (type == typeof(Double))
        return reader.GetDouble(colIndex);
      else if (type == typeof(Int32))
        return reader.GetInt32(colIndex);
      else if (type ==typeof(Int64))
        return reader.GetInt64(colIndex);
      else
        throw new ArgumentException(string.Format(
            "The datatype '{0}' is not supported as a DB datatype", type.Name));
    }
  }
}