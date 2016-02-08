#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.PathManager;

namespace MediaPortal.Plugins.MediaServer.Database
{
  internal class MediaServer_SubSchema
  {
    #region Consts

    public const string SUBSCHEMA_NAME = "MediaServer";

    public const int EXPECTED_SCHEMA_VERSION_MAJOR = 1;
    public const int EXPECTED_SCHEMA_VERSION_MINOR = 0;

    #endregion

    public static string SubSchemaScriptDirectory
    {
      get
      {
        var pathManager = ServiceRegistration.Get<IPathManager>();
        return pathManager.GetPath(@"<APPLICATION_ROOT>\Scripts\");
      }
    }

    public static IDbCommand SelectDlnaMediaInfoCommand(ITransaction transaction, out int systemIdIndex, out int lastHostNameIndex, out int lastClientNameIndex)
    {
      var result = transaction.CreateCommand();

      //result.CommandText = "SELECT SYSTEM_ID, LAST_HOSTNAME, LAST_CLIENT_NAME FROM DLNA_MEDIA_INFO";

      systemIdIndex = 0;
      lastHostNameIndex = 1;
      lastClientNameIndex = 2;
      return result;
    }

    public static IDbCommand InsertAttachedClientCommand(ITransaction transaction, string systemId, string hostName, string clientName)
    {
      var result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO ATTACHED_CLIENTS (SYSTEM_ID, LAST_HOSTNAME, LAST_CLIENT_NAME) VALUES (@SYSTEM_ID, @LAST_HOSTNAME, @LAST_CLIENT_NAME)";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "SYSTEM_ID", systemId, typeof (string));
      database.AddParameter(result, "LAST_HOSTNAME", hostName, typeof (string));
      database.AddParameter(result, "LAST_CLIENT_NAME", clientName, typeof (string));
      return result;
    }

    public static IDbCommand UpdateAttachedClientDataCommand(ITransaction transaction, string systemId,
                                                             SystemName system,
                                                             string clientName)
    {
      var result = transaction.CreateCommand();
      result.CommandText = "UPDATE ATTACHED_CLIENTS SET LAST_HOSTNAME = @LAST_HOSTNAME, LAST_CLIENT_NAME = @LAST_CLIENT_NAME WHERE SYSTEM_ID = @SYSTEM_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "LAST_HOSTNAME", system == null ? null : system.HostName, typeof (string));
      database.AddParameter(result, "LAST_CLIENT_NAME", clientName, typeof (string));
      database.AddParameter(result, "SYSTEM_ID", systemId, typeof (string));
      return result;
    }

    public static IDbCommand DeleteAttachedClientCommand(ITransaction transaction, string systemId)
    {
      var result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM ATTACHED_CLIENTS WHERE SYSTEM_ID = @SYSTEM_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "SYSTEM_ID", systemId, typeof (string));
      return result;
    }
  }
}
