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

using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.AspNetServer
{
  public class AspNetServerSettings
  {
    #region Consts

    public const string HTTPSYS = "Microsoft.AspNetCore.Server.HttpSys";
    public const string KESTREL = "Microsoft.AspNetCore.Server.Kestrel";
    public static readonly string[] SUPPORTED_SERVERS =
    {
      HTTPSYS,
      KESTREL,
    };

    #endregion

    #region Public properties

    /// <summary>
    /// Http-Server to be used by the AspNetServer
    /// </summary>
    [Setting(SettingScope.Global, HTTPSYS)]
    public string Server { get; set; }

    /// <summary>
    /// Indicates whether a very detailed AspNetServerDebug.log is created.
    /// </summary>
#if DEBUG
    [Setting(SettingScope.Global, true)]
#else
    [Setting(SettingScope.Global, false)]
#endif
    public bool EnableDebugLogging { get; set; }

    /// <summary>
    /// Indicates the minimum log level in the AspNetServerDebug.log
    /// We use MP2-LogLevels here.
    /// </summary>
    [Setting(SettingScope.Global, LogLevel.Debug)]
    public LogLevel LogLevel { get; set; }

    #endregion

    #region Public methods

    public string CheckAndGetServer()
    {
      if (SUPPORTED_SERVERS.Contains(Server))
        return Server;
      ServiceRegistration.Get<ILogger>().Warn("AspNetServerSettings: Unknown Server specified ({0}). Using {1} instead.", Server, HTTPSYS);
      return HTTPSYS;
    }

    #endregion
  }
}
