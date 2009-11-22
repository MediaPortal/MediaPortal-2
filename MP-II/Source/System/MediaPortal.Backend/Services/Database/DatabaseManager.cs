#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.Database
{
  public class DatabaseManager : IDatabaseManager, IDisposable
  {
    public void Dispose()
    {
    }

    protected string ExplicitVersionToString(int? versionMajor, int? versionMinor)
    {
      if (!versionMajor.HasValue)
        return "-";
      else if (!versionMinor.HasValue)
        return versionMajor.Value + ".-";
      else
        return versionMajor.Value + "." + versionMinor.Value;
    }

    #region IDatabaseManager implementation

    public void Startup()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>(false);
      if (database == null)
        throw new IllegalCallException("There is no database present in the system");
      // Prepare schema
      if (!database.TableExists(MediaPortal_Basis_Schema.MEDIAPORTAL_BASIS_TABLE_NAME, false))
      {
        ServiceScope.Get<ILogger>().Debug("DatabaseManager: Creating MEDIAPORTAL_BASIS subschema");
        database.ExecuteBatch(new SqlScriptPreprocessor(MediaPortal_Basis_Schema.SubSchemaCreateScriptPath), true);
        ServiceScope.Get<ILogger>().Info("DatabaseManager: MEDIAPORTAL_BASIS subschema created");
      }
      // Hint: Table MEDIAPORTAL_BASIS contains a sub schema entry for "MEDIAPORTAL_BASIS" with version number 1.0
    }

    public ICollection<string> GetDatabaseSubSchemas()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>(false);
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int nameIndex;
        IDbCommand command = MediaPortal_Basis_Schema.SelectAllSubSchemaNames(transaction, out nameIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          ICollection<string> result = new List<string>();
          while (reader.Read())
            result.Add(reader.GetString(nameIndex));
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
        int versionMajorParameterIndex;
        int versionMinorParameterIndex;
        IDbCommand command = MediaPortal_Basis_Schema.SelectVersionBySubschemaCommand(transaction, subSchemaName,
            out versionMajorParameterIndex, out versionMinorParameterIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          if (reader.Read())
          {
            versionMajor = reader.GetInt32(versionMajorParameterIndex);
            versionMinor = reader.GetInt32(versionMinorParameterIndex);
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
        string updateScriptFilePath, int newVersionMajor, int newVersionMinor)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>(false);
      int versionMajor;
      int versionMinor;
      bool schemaPresent = GetSubSchemaVersion(subSchemaName, out versionMajor, out versionMinor);
      if (schemaPresent)
        if (currentVersionMajor.HasValue && currentVersionMajor.Value == versionMajor &&
            currentVersionMinor.HasValue && currentVersionMinor.Value == versionMinor)
        {
          ServiceScope.Get<ILogger>().Debug("DatabaseManager: Updating subschema '{0}' from version {1}.{2} to version {3}.{4}...",
              subSchemaName, versionMajor, versionMinor, newVersionMajor, newVersionMinor);
          database.ExecuteBatch(new SqlScriptPreprocessor(updateScriptFilePath), true);
        }
        else
          throw new ArgumentException(string.Format(
              "The current version of sub schema '{0}' is {1}.{2}, but the schema update script is given for version {3}",
              subSchemaName, versionMajor, versionMinor, ExplicitVersionToString(currentVersionMajor, currentVersionMinor)));
      else // !schemaPresent
        if (!currentVersionMajor.HasValue && !currentVersionMinor.HasValue)
        {
          ServiceScope.Get<ILogger>().Debug("DatabaseManager: Creating subschema '{0}' version {1}.{2}...",
              subSchemaName, newVersionMajor, newVersionMinor);
          database.ExecuteBatch(new SqlScriptPreprocessor(updateScriptFilePath), true);
        }
        else
          throw new ArgumentException(string.Format(
              "The sub schema '{0}' is not present yet, but the schema update script is given for version {1}",
              subSchemaName, ExplicitVersionToString(currentVersionMajor, currentVersionMinor)));
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command;
        if (schemaPresent)
          command = MediaPortal_Basis_Schema.UpdateSubSchemaCommand(
              transaction, subSchemaName, newVersionMajor, newVersionMinor);
        else
          command = MediaPortal_Basis_Schema.InsertSubSchemaCommand(
              transaction, subSchemaName, newVersionMajor, newVersionMinor);
        command.ExecuteNonQuery();
        transaction.Commit();
        ServiceScope.Get<ILogger>().Info("DatabaseManager: Subschema '{0}' present in version {1}.{2}", subSchemaName,
            newVersionMajor, newVersionMinor);
        return true;
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("DatabaseManager: Error updating subschema '{0}'", e, subSchemaName);
        transaction.Rollback();
        throw;
      }
    }

    public void DeleteSubSchema(string subSchemaName, int currentVersionMajor, int currentVersionMinor, string deleteScriptFilePath)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>(false);
      int versionMajor;
      int versionMinor;
      bool schemaPresent = GetSubSchemaVersion(subSchemaName, out versionMajor, out versionMinor);
      if (!schemaPresent)
        return;
      if (currentVersionMajor == versionMajor && currentVersionMinor == versionMinor)
        database.ExecuteBatch(new SqlScriptPreprocessor(deleteScriptFilePath), true);
      else
        throw new ArgumentException(string.Format("The current version of sub schema '{0}' is {1}.{2}, but the schema deletion script works for version {3}.{4}",
            subSchemaName, versionMajor, versionMinor, currentVersionMajor, currentVersionMajor));
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = MediaPortal_Basis_Schema.DeleteSubSchemaCommand(transaction, subSchemaName);
        command.ExecuteNonQuery();
      }
      finally
      {
        transaction.Dispose();
      }
    }

    #endregion
  }
}
