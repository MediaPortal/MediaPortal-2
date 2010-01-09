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

using System.Data;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.PathManager;
using MediaPortal.Backend.Database;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  /// <summary>
  /// Creates SQL commands for the communication with the ClientManager subschema.
  /// </summary>
  public class ClientManager_SubSchema
  {
    #region Consts

    public const string SUBSCHEMA_NAME = "ClientManager";

    public const int EXPECTED_SCHEMA_VERSION_MAJOR = 1;
    public const int EXPECTED_SCHEMA_VERSION_MINOR = 0;

    #endregion

    public static string SubSchemaScriptDirectory
    {
      get
      {
        IPathManager pathManager = ServiceScope.Get<IPathManager>();
        return pathManager.GetPath(@"<APPLICATION_ROOT>\Scripts\");
      }
    }

    public static IDbCommand SelectAttachedClientsCommand(ITransaction transaction, out int attachedClientsIndex,
        out int lastHostNameIndex)
    {
      IDbCommand result = transaction.CreateCommand();

      result.CommandText = "SELECT SYSTEM_ID, LAST_HOSTNAME FROM ATTACHED_CLIENTS";

      attachedClientsIndex = 0;
      lastHostNameIndex = 1;
      return result;
    }

    public static IDbCommand InsertAttachedClientCommand(ITransaction transaction, string systemId, string hostName)
    {
      IDbCommand result = transaction.CreateCommand();

      result.CommandText = "INSERT INTO ATTACHED_CLIENTS (SYSTEM_ID, LAST_HOSTNAME) VALUES (?, ?)";

      IDbDataParameter param = result.CreateParameter();
      param.Value = systemId;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = hostName;
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand UpdateAttachedClientSystemCommand(ITransaction transaction, string systemId, SystemName system)
    {
      IDbCommand result = transaction.CreateCommand();

      result.CommandText = "UPDATE ATTACHED_CLIENTS SET LAST_HOSTNAME = ? WHERE SYSTEM_ID = ?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = system == null ? null : system.HostName;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = systemId;
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand DeleteAttachedClientCommand(ITransaction transaction, string systemId)
    {
      IDbCommand result = transaction.CreateCommand();

      result.CommandText = "DELETE FROM ATTACHED_CLIENTS where SYSTEM_ID = ?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = systemId;
      result.Parameters.Add(param);

      return result;
    }
  }
}
