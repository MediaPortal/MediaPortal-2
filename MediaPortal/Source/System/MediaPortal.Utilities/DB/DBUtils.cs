#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Data;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Utilities.DB
{
  public class DBUtils
  {
    public static DateTime MIN_DB_DATETIME = DateTime.Parse("1900-01-01");
    public static DateTime MAX_DB_DATETIME = DateTime.Parse("2079-06-06");

    public static IDictionary<Type, DbType> DOTNETTYPE2DBTYPE = new Dictionary<Type, DbType>();
    static DBUtils()
    {
      DOTNETTYPE2DBTYPE.Add(typeof(string), DbType.String);
      DOTNETTYPE2DBTYPE.Add(typeof(DateTime), DbType.DateTime);
      DOTNETTYPE2DBTYPE.Add(typeof(Char), DbType.String);
      DOTNETTYPE2DBTYPE.Add(typeof(Boolean), DbType.Boolean);
      DOTNETTYPE2DBTYPE.Add(typeof(Single), DbType.Single);
      DOTNETTYPE2DBTYPE.Add(typeof(Double), DbType.Double);
      DOTNETTYPE2DBTYPE.Add(typeof(Byte), DbType.Byte);
      DOTNETTYPE2DBTYPE.Add(typeof(SByte), DbType.SByte);
      DOTNETTYPE2DBTYPE.Add(typeof(UInt16), DbType.UInt16);
      DOTNETTYPE2DBTYPE.Add(typeof(Int16), DbType.Int16);
      DOTNETTYPE2DBTYPE.Add(typeof(UInt32), DbType.UInt32);
      DOTNETTYPE2DBTYPE.Add(typeof(Int32), DbType.Int32);
      DOTNETTYPE2DBTYPE.Add(typeof(UInt64), DbType.UInt64);
      DOTNETTYPE2DBTYPE.Add(typeof(Int64), DbType.Int64);
      DOTNETTYPE2DBTYPE.Add(typeof(Guid), DbType.Guid);
    }

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
      if (type == typeof(DateTime))
        return reader.GetDateTime(colIndex);
      if (type == typeof(Char))
        return reader.GetChar(colIndex);
      if (type == typeof(Boolean))
        return reader.GetBoolean(colIndex);
      if (type == typeof(Single))
        return reader.GetFloat(colIndex);
      if (type == typeof(Double))
        return reader.GetDouble(colIndex);
      if (type == typeof(Int32))
        return reader.GetInt32(colIndex);
      if (type == typeof(Int64))
        return reader.GetInt64(colIndex);
      if (type == typeof(Guid))
        return reader.GetGuid(colIndex);
      throw new ArgumentException(string.Format(
          "The datatype '{0}' is not supported as a DB datatype", type.Name));
    }

    public static DbType GetDBType(Type dotNetType)
    {
      DbType result;
      if (DOTNETTYPE2DBTYPE.TryGetValue(dotNetType, out result))
        return result;
      throw new InvalidDataException("A type mapping to a DB type for the dot net type '{0}' is not supported", dotNetType);
    }

    public static void CheckValueConstraints(ref object value)
    {
      if (value is DateTime)
      {
        DateTime dt = (DateTime) value;
        if (dt < MIN_DB_DATETIME)
          value = MIN_DB_DATETIME;
        else if (dt > MAX_DB_DATETIME)
          value = MAX_DB_DATETIME;
      }
    }

    /// <summary>
    /// Initializes a bind variable in the given DB <paramref name="command"/>.
    /// </summary>
    /// <param name="command">Command where the bind variable should be initialized.</param>
    /// <param name="name">Name of the bind variable. If the variable name is <c>XXX</c>, the variable should be
    /// referenced as <c>@XXX</c> in the <see cref="IDbCommand.CommandText"/>.</param>
    /// <param name="value">Actual value of the bind variable.</param>
    /// <param name="type">Type of the variable in the database. In most cases, this is the trivial mapping from
    /// <c>typeof(value)</c> to <see cref="DbType"/>, only if <c>value == null</c> or <c>value is string</c>,
    /// the <see cref="DbType"/> gives us more information than we can get from <paramref name="value"/>.</param>
    /// <returns>Created parameter.</returns>
    public static IDbDataParameter AddParameter(IDbCommand command, string name, object value, DbType type)
    {
      IDbDataParameter result = command.CreateParameter();
      result.ParameterName = name;
      CheckValueConstraints(ref value);
      result.Value = value ?? DBNull.Value;
      result.DbType = type;
      command.Parameters.Add(result);
      return result;
    }
  }
}