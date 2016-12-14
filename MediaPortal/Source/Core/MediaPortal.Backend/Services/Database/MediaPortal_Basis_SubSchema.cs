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

using System.Data;
using System.IO;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;

namespace MediaPortal.Backend.Services.Database
{
  /// <summary>
  /// Creates SQL commands for the communication with the MEDIAPORTAL_BASIS sub schema.
  /// </summary>
  public class MediaPortal_Basis_Schema
  {
    public const string SUBSCHEMA_NAME = "MediaPortal-Basis";

    public const string MEDIAPORTAL_BASIS_TABLE_NAME = "MEDIAPORTAL_BASIS";
    public const string SUBSCHEMA_CREATE_SCRIPT_NAME = "MediaPortal-Basis-create-1.0.sql";

    public static string SubSchemaCreateScriptPath
    {
      get
      {
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        return Path.Combine(pathManager.GetPath(@"<APPLICATION_ROOT>\Scripts\"), SUBSCHEMA_CREATE_SCRIPT_NAME);
      }
    }

    public static IDbCommand SelectAllSubSchemaNames(ITransaction transaction, out int nameIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT SUBSCHEMA_NAME FROM MEDIAPORTAL_BASIS";

      nameIndex = 0;
      return result;
    }

    public static IDbCommand SelectVersionBySubschemaCommand(ITransaction transaction, string subSchemaName,
      out int versionMajorParameterIndex, out int versionMinorParameterIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT VERSION_MAJOR, VERSION_MINOR FROM MEDIAPORTAL_BASIS WHERE SUBSCHEMA_NAME = @SUBSCHEMA_NAME";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "SUBSCHEMA_NAME", subSchemaName, typeof(string));
      versionMajorParameterIndex = 0;
      versionMinorParameterIndex = 1;
      return result;
    }

    public static IDbCommand UpdateSubSchemaCommand(ITransaction transaction, string subSchemaName,
        int versionMajor, int versionMinor)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "UPDATE MEDIAPORTAL_BASIS SET VERSION_MAJOR=@VERSION_MAJOR, VERSION_MINOR=@VERSION_MINOR WHERE SUBSCHEMA_NAME=@SUBSCHEMA_NAME";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "VERSION_MAJOR", versionMajor, typeof(int));
      database.AddParameter(result, "VERSION_MINOR", versionMinor, typeof(int));
      database.AddParameter(result, "SUBSCHEMA_NAME", subSchemaName, typeof(string));
      return result;
    }

    public static IDbCommand InsertSubSchemaCommand(ITransaction transaction, string subSchemaName,
        int versionMajor, int versionMinor)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO MEDIAPORTAL_BASIS (SUBSCHEMA_NAME, VERSION_MAJOR, VERSION_MINOR) VALUES (@SUBSCHEMA_NAME, @VERSION_MAJOR, @VERSION_MINOR)";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "SUBSCHEMA_NAME", subSchemaName, typeof(string));
      database.AddParameter(result, "VERSION_MAJOR", versionMajor, typeof(int));
      database.AddParameter(result, "VERSION_MINOR", versionMinor, typeof(int));
      return result;
    }

    public static IDbCommand DeleteSubSchemaCommand(ITransaction transaction, string subSchemaName)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM MEDIAPORTAL_BASIS WHERE SUBSCHEMA_NAME=@SUBSCHEMA_NAME";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "SUBSCHEMA_NAME", subSchemaName, typeof(string));
      return result;
    }
  }
}
