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

using System.Collections.Generic;
using System.Data;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Database;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Services.Database
{
  public class DatabaseManager : IDatabaseManager
  {
    public const string SUBSCHEMAS_TABLE = "MEDIAPORTAL_BASIS";
    public const string SUBSCHEMAS_SUBSCHEMA_NAME = "SUBSCHEMA_NAME";
    public const string SUBSCHEMAS_VERSION_MAJOR = "VERSION_MAJOR";
    public const string SUBSCHEMAS_VERSION_MINOR = "VERSION_MINOR";

    public const string SUBSCHEMA_SCRIPT_NAME = "MediaPortal-Basis-create-1.0.sql";

    public void Startup()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>(false);
      if (database == null)
        throw new IllegalCallException("There is no database present in the system");
      // Prepare schema
      if (!database.TableExists(SUBSCHEMAS_TABLE, false))
      {
        IPathManager pathManager = ServiceScope.Get<IPathManager>();
        string scriptPath = Path.Combine(pathManager.GetPath(@"<APPLICATION_ROOT>\Base\Scripts\"), SUBSCHEMA_SCRIPT_NAME);
        database.ExecuteBatch(scriptPath, true);
      }
    }

    public void Shutdown()
    {
    }

    #region IDatabaseManager implementation

    public ICollection<string> GetDatabaseSubSchemas()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>(false);
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = transaction.CreateCommand();
        command.CommandText = "SELECT " + SUBSCHEMAS_SUBSCHEMA_NAME + " FROM " + SUBSCHEMAS_TABLE;
        IDataReader reader = command.ExecuteReader();
        try
        {
          ICollection<string> result = new List<string>();
          while (reader.Read())
            result.Add(reader.GetString(0));
          return result;
        }
        finally
        {
          reader.Close();
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public bool GetSubSchemaVersion(string subSchemaName, out int versionMajor, out int versionMinor)
    {
      versionMajor = 0;
      versionMinor = 0;
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>(false);
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = transaction.CreateCommand();
        command.CommandText = "SELECT " + SUBSCHEMAS_VERSION_MAJOR + ", " + SUBSCHEMAS_VERSION_MINOR + " FROM " + SUBSCHEMAS_TABLE +
            " WHERE " + SUBSCHEMAS_SUBSCHEMA_NAME + "='" + subSchemaName + "'";
        IDataReader reader = command.ExecuteReader();
        try
        {
          if (reader.Read())
          {
            versionMajor = reader.GetInt32(0);
            versionMajor = reader.GetInt32(1);
            return true;
          }
          return false;
        }
        finally
        {
          reader.Close();
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public bool UpdateSubSchema(string subSchemaName, int? currentVersionMajor, int? currentVersionMinor,
        string updateScript, int newVersionMajor, int newVersionMinor)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>(false);
      int versionMajor;
      int versionMinor;
      bool schemaPresent = GetSubSchemaVersion(subSchemaName, out versionMajor, out versionMinor);
      if (schemaPresent &&
          currentVersionMajor.HasValue && currentVersionMajor.Value == versionMajor &&
          currentVersionMinor.HasValue && currentVersionMinor.Value == versionMinor ||
          !schemaPresent && !currentVersionMajor.HasValue && !currentVersionMinor.HasValue)
        database.ExecuteBatch(updateScript, true);
      else
        return false;
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = transaction.CreateCommand();
        if (schemaPresent)
          command.CommandText = "UPDATE " + SUBSCHEMAS_TABLE + " SET " +
              SUBSCHEMAS_VERSION_MAJOR + "=" + newVersionMajor + ", " +
              SUBSCHEMAS_VERSION_MINOR + "=" + newVersionMinor +
              " WHERE " + SUBSCHEMAS_SUBSCHEMA_NAME + "='" + subSchemaName + "'";
        else
          command.CommandText = "INSERT INTO " + SUBSCHEMAS_TABLE + " (" +
              SUBSCHEMAS_SUBSCHEMA_NAME + ", " + SUBSCHEMAS_VERSION_MAJOR + "," + SUBSCHEMAS_VERSION_MINOR + ") VALUES (" +
              subSchemaName + ", " + newVersionMajor + ", " + newVersionMinor + ")";
        command.ExecuteNonQuery();
        return true;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    #endregion
  }
}
