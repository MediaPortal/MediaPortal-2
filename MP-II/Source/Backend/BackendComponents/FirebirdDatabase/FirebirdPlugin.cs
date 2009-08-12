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

using System;
using System.Data;
using System.IO;
using FirebirdSql.Data.FirebirdClient;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Settings;
using MediaPortal.Database.Firebird.Settings;

namespace MediaPortal.Database.Firebird
{
  public class FirebirdPlugin : IPluginStateTracker
  {
    protected FirebirdSQLDatabase _database;

    public void Activated(PluginRuntime pluginRuntime)
    {
      FirebirdSettings settings = ServiceScope.Get<ISettingsManager>().Load<FirebirdSettings>();
      FbConnectionStringBuilder sb = new FbConnectionStringBuilder
        {
            ServerType = settings.ServerType,
            UserID = settings.UserID,
            Password = settings.Password,
            Dialect = 3,
            Database = settings.DatabaseFile
        };
      if (!File.Exists(settings.DatabaseFile))
        FbConnection.CreateDatabase(settings.DatabaseFile);
      FbConnection con = new FbConnection(sb.ConnectionString);
      _database = new FirebirdSQLDatabase(con);
      try
      {
        con.Open();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Critical("Error opening database '{0}'", e, settings.DatabaseFile);
        throw;
      }
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      if (_database == null)
        return;
      _database.Dispose();
      _database = null;
    }

    public void Continue() { }

    public void Shutdown()
    {
      if (_database == null)
        return;
      _database.Dispose();
      _database = null;
    }
  }
}
