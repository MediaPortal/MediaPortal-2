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

using System.Data;
using System.IO;
using MediaPortal.Database;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;

namespace MediaPortal.Services.Database
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
        IPathManager pathManager = ServiceScope.Get<IPathManager>();
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
      result.CommandText = "SELECT VERSION_MAJOR, VERSION_MINOR FROM MEDIAPORTAL_BASIS WHERE SUBSCHEMA_NAME=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = subSchemaName;
      result.Parameters.Add(param);

      versionMajorParameterIndex = 0;
      versionMinorParameterIndex = 1;
      return result;
    }

    public static IDbCommand UpdateSubSchemaCommand(ITransaction transaction, string subSchemaName,
        int versionMajor, int versionMinor)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "UPDATE MEDIAPORTAL_BASIS SET VERSION_MAJOR=?, VERSION_MINOR=? WHERE SUBSCHEMA_NAME=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = versionMajor;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = versionMinor;
      result.Parameters.Add(param);
      
      param = result.CreateParameter();
      param.Value = subSchemaName;
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand InsertSubSchemaCommand(ITransaction transaction, string subSchemaName,
        int versionMajor, int versionMinor)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO MEDIAPORTAL_BASIS (SUBSCHEMA_NAME, VERSION_MAJOR, VERSION_MINOR) VALUES (?, ?, ?)";

      IDbDataParameter param = result.CreateParameter();
      param.Value = subSchemaName;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = versionMajor;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = versionMinor;
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand DeleteSubSchemaCommand(ITransaction transaction, string subSchemaName)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM MEDIAPORTAL_BASIS WHERE SUBSCHEMA_NAME=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = subSchemaName;
      result.Parameters.Add(param);

      return result;
    }
  }
}
