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
using System;

namespace MediaPortal.Plugins.MediaServer.Database
{
  internal class MediaServer_SubSchema
  {
    public static IDbCommand UpdateAttachedClientDataCommand(ITransaction transaction, string systemId, string hostName,
			string clientName)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "UPDATE ATTACHED_CLIENTS SET LAST_HOSTNAME = @LAST_HOSTNAME, LAST_CLIENT_NAME = @LAST_CLIENT_NAME WHERE SYSTEM_ID = @SYSTEM_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "LAST_HOSTNAME", hostName, typeof (string));
      database.AddParameter(result, "LAST_CLIENT_NAME", clientName, typeof (string));
      database.AddParameter(result, "SYSTEM_ID", systemId, typeof (string));
      return result;
    }
  }
}
