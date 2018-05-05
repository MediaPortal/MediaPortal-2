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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;

namespace MediaPortal.Database.MSSQL
{
  public class MSSQLDatabaseSettings
  {
    #region Constants

    public const string MSSQL_DATABASE_TYPE = "MSSQL";
    public const int MAX_NUM_CHARS_CHAR_VARCHAR = 4000;
    public const int MAX_CONNECTION_POOL_SIZE = 5000;
    public const bool USE_CONNECTION_POOL = true;
    public const int DEFAULT_CONNECTION_TIMEOUT = 15;
    public const int DEFAULT_QUERY_TIMEOUT = 30;
    public const int INITIAL_LOG_SIZE = 50;
    public const int INITIAL_DATA_SIZE = 200;
    public const int LOG_GROWTH_SIZE = 25;
    public const int DATA_GROWTH_SIZE = 100;
    public const string DEFAULT_DATABASE_FILE = "MP2Datastore.mdf";
    public const string DEFAULT_DATABASE_LOG_FILE = "MP2Datastore.ldf";
    public const string DEFAULT_DATABASE_INSTANCE = @".\SQLExpress";
    public const string DEFAULT_DATABASE_USER = "MPUser";
    public const string DEFAULT_DATABASE_USER_PASSWORD = "MediaPortal";
    public const string DEFAULT_DATABASE_NAME = "MP2Datastore";

    #endregion

    #region Public properties

    [Setting(SettingScope.Global, USE_CONNECTION_POOL)]
    public bool UseConnectionPool { get; set; }

    [Setting(SettingScope.Global, MAX_CONNECTION_POOL_SIZE)]
    public int MaxConnectionPoolSize { get; set; }

    [Setting(SettingScope.Global, DEFAULT_DATABASE_FILE)]
    public string DatabaseFileName { get; set; }

    [Setting(SettingScope.Global, DEFAULT_DATABASE_LOG_FILE)]
    public string DatabaseLogFileName { get; set; }

    [Setting(SettingScope.Global, DATA_GROWTH_SIZE)]
    public int DatabaseFileGrowSize { get; set; }

    [Setting(SettingScope.Global, LOG_GROWTH_SIZE)]
    public int DatabaseLogFileGrowSize { get; set; }

    [Setting(SettingScope.Global, DEFAULT_DATABASE_INSTANCE)]
    public string DatabaseInstance { get; set; }

    [Setting(SettingScope.Global, DEFAULT_DATABASE_USER)]
    public string DatabaseUser { get; set; }

    [Setting(SettingScope.Global, DEFAULT_DATABASE_USER_PASSWORD)]
    public string DatabasePassword { get; set; }

    [Setting(SettingScope.Global, DEFAULT_DATABASE_NAME)]
    public string DatabaseName { get; set; }

    [Setting(SettingScope.Global, false)]
    public bool EnableDebugLogging { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Logs all the settings contained in this class to the Logfile
    /// </summary>
    public void LogSettings()
    {
      ServiceRegistration.Get<ILogger>().Info("MSSQLDatabase: Database Instance: '{0}' (Default: '{1}')", DatabaseInstance, DEFAULT_DATABASE_INSTANCE);
      ServiceRegistration.Get<ILogger>().Info("MSSQLDatabase: Database User: '{0}' (Default: '{1}')", DatabaseUser, DEFAULT_DATABASE_USER);
      ServiceRegistration.Get<ILogger>().Info("MSSQLDatabase: Database Name: '{0}' (Default: '{1}')", DatabaseName, DEFAULT_DATABASE_NAME);
      ServiceRegistration.Get<ILogger>().Info("MSSQLDatabase: Database Log Filename: '{0}' (Default: '{1}')", DatabaseLogFileName, DEFAULT_DATABASE_LOG_FILE);
      ServiceRegistration.Get<ILogger>().Info("MSSQLDatabase: Database Filename Grow Size: {0} MB (Default: {1} MB)", DatabaseFileGrowSize, DATA_GROWTH_SIZE);
      ServiceRegistration.Get<ILogger>().Info("MSSQLDatabase: Database Log Filename Grow Size: {0} MB (Default: {1} MB)", DatabaseLogFileGrowSize, LOG_GROWTH_SIZE);
      ServiceRegistration.Get<ILogger>().Info("MSSQLDatabase: Use Connection Pool: {0} (Default: {1})", UseConnectionPool, USE_CONNECTION_POOL);
      ServiceRegistration.Get<ILogger>().Info("MSSQLDatabase: Max Connection Pool Size: {0} (Default: {1})", MaxConnectionPoolSize, MAX_CONNECTION_POOL_SIZE);
    }

    #endregion
  }
}
