#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Backend.Database;

namespace MediaPortal.Backend.Services.Database
{
  /// <summary>
  /// Extension methods for <see cref="ISQLDatabase"/>.
  /// </summary>
  public static class SQLDatabaseExtension
  {
    /// <summary>
    /// Reads a value of the given type from the given database <paramref name="reader"/> from the column of the given <paramref name="colIndex"/>.
    /// </summary>
    /// <typeparam name="T">Type of the parameter to read. The data access layer will automatically try to convert a value of a convertible
    /// format, if possible.</typeparam>
    /// <param name="database">Underlaying SQL database.</param>
    /// <param name="reader">Reader containing the value to read.</param>
    /// <param name="colIndex">Index of the column to read. Indices start at <c>0</c>.</param>
    /// <returns>Value which was read or <c>null</c>.</returns>
    public static T ReadDBValue<T>(this ISQLDatabase database, IDataReader reader, int colIndex)
    {
      return (T) database.ReadDBValue(typeof(T), reader, colIndex);
    }
  }

  /// <summary>
  /// Support implementation which provides default handling for simple datatypes.
  /// </summary>
  public abstract class DBUtils
  {
    public static DateTime MIN_DB_DATETIME = new DateTime(1900, 1, 1);
    public static DateTime MAX_DB_DATETIME = new DateTime(2079, 6, 6);

    protected static IDictionary<Type, DbType> SIMPLEDOTNETTYPE2DBTYPE = new Dictionary<Type, DbType>();
    static DBUtils()
    {
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(string), DbType.String);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(DateTime), DbType.DateTime);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(Char), DbType.String);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(Boolean), DbType.Boolean);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(Single), DbType.Single);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(Double), DbType.Double);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(Byte), DbType.Byte);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(SByte), DbType.SByte);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(UInt16), DbType.UInt16);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(Int16), DbType.Int16);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(UInt32), DbType.UInt32);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(Int32), DbType.Int32);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(UInt64), DbType.UInt64);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(Int64), DbType.Int64);
      SIMPLEDOTNETTYPE2DBTYPE.Add(typeof(Guid), DbType.Guid);
    }

    protected static void CheckValueConstraints(ref object value)
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
    /// <remarks>
    /// This method supports the types
    /// <list>
    /// <item><see cref="String"/></item>
    /// <item><see cref="DateTime"/></item>
    /// <item><see cref="Char"/></item>
    /// <item><see cref="Boolean"/></item>
    /// <item><see cref="Single"/></item>
    /// <item><see cref="Double"/></item>
    /// <item><see cref="Int32"/></item>
    /// <item><see cref="Int64"/></item>
    /// <item><see cref="Guid"/></item>
    /// </list>
    /// The type <c>byte[]</c> is NOT supported here.
    /// </remarks>
    /// <param name="command">Command where the bind variable should be initialized.</param>
    /// <param name="name">Name of the bind variable.</param>
    /// <param name="value">Actual value of the bind variable.</param>
    /// <param name="type">Type of the variable.</param>
    /// <returns>Created parameter.</returns>
    public static IDbDataParameter AddSimpleParameter(IDbCommand command, string name, object value, Type type)
    {
      IDbDataParameter result = command.CreateParameter();
      result.ParameterName = name;
      CheckValueConstraints(ref value);
      result.Value = value ?? DBNull.Value;
      DbType dbType;
      if (SIMPLEDOTNETTYPE2DBTYPE.TryGetValue(type, out dbType))
        result.DbType = dbType;
      else
        throw new ArgumentException(string.Format("The datatype '{0}' is not supported as a DB datatype", type.Name));
      command.Parameters.Add(result);
      return result;
    }

    /// <summary>
    /// Reads a value from the given DB data <paramref name="reader"/> from the column of the given <paramref name="colIndex"/>.
    /// </summary>
    /// <remarks>
    /// This method supports the types
    /// <list>
    /// <item><see cref="String"/></item>
    /// <item><see cref="DateTime"/></item>
    /// <item><see cref="Char"/></item>
    /// <item><see cref="Boolean"/></item>
    /// <item><see cref="Single"/></item>
    /// <item><see cref="Double"/></item>
    /// <item><see cref="Int32"/></item>
    /// <item><see cref="Int64"/></item>
    /// <item><see cref="Guid"/></item>
    /// </list>
    /// The type <c>byte[]</c> is NOT supported here.
    /// </remarks>
    /// <param name="type">Type of the parameter to read.</param>
    /// <param name="reader">Reader to take the value from.</param>
    /// <param name="colIndex">Index of the column in the query result which is represented by the
    /// <paramref name="reader"/>.</param>
    /// <returns>Read value.</returns>
    public static object ReadSimpleDBValue(Type type, IDataReader reader, int colIndex)
    {
      if (type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        type = type.GetGenericArguments()[0];
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
  }
}