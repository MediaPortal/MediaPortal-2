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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities.DB;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.Database
{
  public class DatabaseManager : IDatabaseManager, IDisposable
  {
    public const string DUMMY_TABLE_NAME = "DUMMY";

    public void Dispose()
    {
    }

    protected string ExplicitVersionToString(int? versionMajor, int? versionMinor)
    {
      if (!versionMajor.HasValue)
        return "-";
      if (!versionMinor.HasValue)
        return versionMajor.Value + ".-";
      return versionMajor.Value + "." + versionMinor.Value;
    }

    #region IDatabaseManager implementation

    public string DummyTableName
    {
      get { return DUMMY_TABLE_NAME; }
    }

    public void Startup()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      if (database == null)
        throw new IllegalCallException("There is no database present in the system");
      // Prepare schema
      if (!database.TableExists(MediaPortal_Basis_Schema.MEDIAPORTAL_BASIS_TABLE_NAME))
      {
        ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Creating subschema '{0}'", MediaPortal_Basis_Schema.SUBSCHEMA_NAME);
        using (TextReader reader = new SqlScriptPreprocessor(MediaPortal_Basis_Schema.SubSchemaCreateScriptPath))
          ExecuteBatch(database, new InstructionList(reader));
      }
      // Hint: Table MEDIAPORTAL_BASIS contains a sub schema entry for "MEDIAPORTAL_BASIS" with version number 1.0
      int versionMajor;
      int versionMinor;
      if (!GetSubSchemaVersion(MediaPortal_Basis_Schema.SUBSCHEMA_NAME, out versionMajor, out versionMinor))
        throw new UnexpectedStateException("{0} schema is not present or corrupted", MediaPortal_Basis_Schema.SUBSCHEMA_NAME);
      ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Subschema '{0}' present in version {1}.{2}",
          MediaPortal_Basis_Schema.SUBSCHEMA_NAME, versionMajor, versionMinor);
    }

    public ICollection<string> GetDatabaseSubSchemas()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int nameIndex;
        using (IDbCommand command = MediaPortal_Basis_Schema.SelectAllSubSchemaNames(transaction, out nameIndex))
        using (IDataReader reader = command.ExecuteReader())
        {
          ICollection<string> result = new List<string>();
          while (reader.Read())
            result.Add(database.ReadDBValue<string>(reader, nameIndex));
          return result;
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
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int versionMajorParameterIndex;
        int versionMinorParameterIndex;
        using (IDbCommand command = MediaPortal_Basis_Schema.SelectVersionBySubschemaCommand(transaction, subSchemaName,
            out versionMajorParameterIndex, out versionMinorParameterIndex))
        using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
        {
          if (reader.Read())
          {
            // Versions are marked NOT NULL, so it is safe not to check for DBNull
            versionMajor = reader.GetInt32(versionMajorParameterIndex);
            versionMinor = reader.GetInt32(versionMinorParameterIndex);
            return true;
          }
          return false;
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
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      int versionMajor;
      int versionMinor;
      bool schemaPresent = GetSubSchemaVersion(subSchemaName, out versionMajor, out versionMinor);
      if (schemaPresent)
        if (currentVersionMajor.HasValue && currentVersionMajor.Value == versionMajor &&
            currentVersionMinor.HasValue && currentVersionMinor.Value == versionMinor)
        {
          ServiceRegistration.Get<ILogger>().Debug("DatabaseManager: Updating subschema '{0}' from version {1}.{2} to version {3}.{4}...",
              subSchemaName, versionMajor, versionMinor, newVersionMajor, newVersionMinor);
          using (TextReader reader = new SqlScriptPreprocessor(updateScriptFilePath))
            ExecuteBatch(database, new InstructionList(reader));
        }
        else
          throw new ArgumentException(string.Format(
              "The current version of sub schema '{0}' is {1}.{2}, but the schema update script is given for version {3}",
              subSchemaName, versionMajor, versionMinor, ExplicitVersionToString(currentVersionMajor, currentVersionMinor)));
      else // !schemaPresent
        if (!currentVersionMajor.HasValue && !currentVersionMinor.HasValue)
        {
          ServiceRegistration.Get<ILogger>().Debug("DatabaseManager: Creating subschema '{0}' version {1}.{2}...",
              subSchemaName, newVersionMajor, newVersionMinor);
          using (TextReader reader = new SqlScriptPreprocessor(updateScriptFilePath))
            ExecuteBatch(database, new InstructionList(reader));
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
        try
        {
          command.ExecuteNonQuery();
        }
        finally
        {
          command.Dispose();
        }
        transaction.Commit();
        ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Subschema '{0}' present in version {1}.{2}", subSchemaName,
            newVersionMajor, newVersionMinor);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("DatabaseManager: Error updating subschema '{0}'", e, subSchemaName);
        transaction.Rollback();
        throw;
      }
    }

    public void DeleteSubSchema(string subSchemaName, int currentVersionMajor, int currentVersionMinor, string deleteScriptFilePath)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      int versionMajor;
      int versionMinor;
      bool schemaPresent = GetSubSchemaVersion(subSchemaName, out versionMajor, out versionMinor);
      if (!schemaPresent)
        return;
      if (currentVersionMajor == versionMajor && currentVersionMinor == versionMinor)
        using (TextReader reader = new SqlScriptPreprocessor(deleteScriptFilePath))
          ExecuteBatch(database, new InstructionList(reader));
      else
        throw new ArgumentException(string.Format("The current version of sub schema '{0}' is {1}.{2}, but the schema deletion script works for version {3}.{4}",
            subSchemaName, versionMajor, versionMinor, currentVersionMajor, currentVersionMajor));
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = MediaPortal_Basis_Schema.DeleteSubSchemaCommand(transaction, subSchemaName))
          command.ExecuteNonQuery();
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public void ExecuteBatch(ISQLDatabase database, InstructionList instructions)
    {
      using (ITransaction transaction = database.BeginTransaction())
      {
        foreach (string instr in instructions)
          using (IDbCommand cmd = transaction.CreateCommand())
          {
            cmd.CommandText = instr;
            cmd.ExecuteNonQuery();
          }
        transaction.Commit();
      }
    }

    #endregion
  }
}
