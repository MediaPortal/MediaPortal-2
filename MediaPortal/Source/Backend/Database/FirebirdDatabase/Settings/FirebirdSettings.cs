#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.IO;
using FirebirdSql.Data.FirebirdClient;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;

namespace MediaPortal.Database.Firebird.Settings
{
  public class FirebirdSettings
  {
    #region Consts

    public const FbServerType DEFAULT_SERVER_TYPE = FbServerType.Embedded;
    public const string DEFAULT_DATABASE_FILE = "Datastore.fdb";
    public const string DEFAULT_USER_ID = "MediaPortal";
    public const string DEFAULT_PASSWORD = "Firebird";
    public const int DEFAULT_NUM_CONNECTIONS = 5;

    #endregion

    #region Protected fields

    protected FbServerType _serverType = DEFAULT_SERVER_TYPE;
    protected string _databaseFile;
    protected string _userID = DEFAULT_USER_ID;
    protected string _password = DEFAULT_PASSWORD;
    protected int _numConnections = DEFAULT_NUM_CONNECTIONS;

    #endregion

    public FirebirdSettings()
    {
      IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
      string dataDirectory = pathManager.GetPath("<DATA>");
      _databaseFile = Path.Combine(dataDirectory, DEFAULT_DATABASE_FILE);
    }

    [Setting(SettingScope.Global)]
    public FbServerType ServerType
    {
      get { return _serverType; }
      set { _serverType = value; }
    }

    [Setting(SettingScope.Global)]
    public string DatabaseFile
    {
      get { return _databaseFile; }
      set { _databaseFile = value; }
    }

    [Setting(SettingScope.Global, DEFAULT_USER_ID)]
    public string UserID
    {
      get { return _userID; }
      set { _userID = value; }
    }

    [Setting(SettingScope.Global, DEFAULT_PASSWORD)]
    public string Password
    {
      get { return _password; }
      set { _password = value; }
    }

    [Setting(SettingScope.Global, DEFAULT_NUM_CONNECTIONS)]
    public int NumConnections
    {
      get { return _numConnections; }
      set { _numConnections = value; }
    }
  }
}
