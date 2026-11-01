#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Data;
using Microsoft.Data.Sqlite;

namespace MediaPortal.Database.SQLite
{
  /// <summary>
  /// The one place that decides how a .NET value is handed to SQLite, so that the decision can be
  /// tested against a real database without constructing the whole provider.
  /// </summary>
  /// <remarks>
  /// This exists because the driver swap changed two storage defaults, and one of them changes
  /// results rather than throwing:
  /// <list type="bullet">
  /// <item>
  /// <description>Guids. The databases this provider has to keep reading were written by
  /// System.Data.SQLite with <c>BinaryGUID=true</c>, which stored them as 16-byte BLOBs - in
  /// <see cref="Guid.ToByteArray"/> byte order, verified against a real MP2 database where
  /// MIA_TYPES.MIAM_ID is declared TEXT but stored as <c>typeof=blob, length 16</c>.
  /// Microsoft.Data.Sqlite binds a Guid as TEXT, so such a parameter matches none of those rows.
  /// Measured on that same database: <c>WHERE MIAM_ID=@g</c> with a Guid returned 0 rows, with the
  /// bytes it returned 1. An empty result set instead of an error is the most expensive way a storage
  /// format can differ, so Guids are bound as bytes here.</description>
  /// </item>
  /// <item>
  /// <description>Byte arrays, which are BLOBs; kept as it was.</description>
  /// </item>
  /// </list>
  /// Everything else goes through <see cref="MediaPortal.Backend.Services.Database.DBUtils"/>, which
  /// is shared with the other providers - that is why the Guid case is handled here and not there.
  /// </remarks>
  public static class SQLiteValues
  {
    /// <summary>
    /// Converts a value to the representation this provider stores, without touching the command.
    /// </summary>
    /// <param name="type">The .NET type the database layer says the value has.</param>
    /// <param name="value">The value, possibly <see cref="DBNull.Value"/> or null.</param>
    /// <returns>The object to bind.</returns>
    public static object ConvertParameter(Type type, object value)
    {
      if (value == null || value is DBNull)
        return DBNull.Value;
      if (type == typeof(Guid))
        return ((Guid)value).ToByteArray();
      return value;
    }

    /// <summary>
    /// Adds a BLOB-typed parameter for the types this provider stores as BLOBs.
    /// </summary>
    /// <param name="command">Command to add the parameter to.</param>
    /// <param name="name">Parameter name.</param>
    /// <param name="value">Value to bind.</param>
    /// <param name="type">Declared .NET type of the value.</param>
    /// <returns>The parameter, or <c>null</c> if this binder does not handle <paramref name="type"/>
    /// and the caller should fall back to the shared binder.</returns>
    public static SqliteParameter TryAddBinaryParameter(IDbCommand command, string name, object value, Type type)
    {
      if (type != typeof(byte[]) && type != typeof(Guid))
        return null;

      var result = new BinaryGuidParameter
      {
        ParameterName = name,
        DbType = DbType.Binary,
        Value = ConvertParameter(type, value),
      };
      command.Parameters.Add(result);
      return result;
    }

    /// <summary>
    /// A BLOB parameter that stores a Guid as its sixteen bytes however the value arrives - when the
    /// parameter is made, and when it is written again afterwards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TryAddBinaryParameter"/> converts the value it is handed, but the database layer's
    /// habit is to build a parameter once and then write <c>Value</c> again for every statement or
    /// every row: <c>MediaLibrary.UpdateParentPlayUserData</c> creates an item id and a profile id this
    /// way and reassigns both for every parent of every reported title. A plain parameter takes what it
    /// is given and lets the driver pick a representation, and the driver picks text for a Guid - which
    /// is no id anywhere in this database (see the remarks on <see cref="SQLiteValues"/>).
    /// </para>
    /// <para>
    /// Measured on a real library, that read as follows: the update meant to meet a parent's existing
    /// row met nothing, the insert behind it failed the foreign key onto the media items
    /// (<c>SQLite Error 19: 'FOREIGN KEY constraint failed'</c>), the front end's report of a watched
    /// film came back as a server error, and no series or album ever received the watched state its
    /// episodes and tracks had earned. Converting at the assignment keeps the storage decision inside
    /// the one class that owns it and takes the trap away from every caller at once.
    /// </para>
    /// </remarks>
    public class BinaryGuidParameter : SqliteParameter
    {
      public override object Value
      {
        get { return base.Value; }
        set { base.Value = value is Guid guid ? guid.ToByteArray() : value; }
      }
    }
  }
}
