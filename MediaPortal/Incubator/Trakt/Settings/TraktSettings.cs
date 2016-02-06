#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.IO;
using System.Reflection;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;

namespace MediaPortal.UiComponents.Trakt.Settings
{
  public class TraktSettings
  {
    [Setting(SettingScope.User)]
    public bool EnableTrakt { get; set; }

    [Setting(SettingScope.User, DefaultValue = "")]
    public string Username { get; set; }

    [Setting(SettingScope.User, DefaultValue = "")]
    public string TraktOAuthToken { get; set; }

    [Setting(SettingScope.User, DefaultValue = false)]
    public bool UseSSL { get; set; }

    [Setting(SettingScope.User, DefaultValue = false)]
    public bool KeepTraktLibraryClean { get; set; }

    [Setting(SettingScope.User, DefaultValue = 0)]
    public int TrendingMoviesDefaultLayout { get; set; }

    [Setting(SettingScope.User, DefaultValue = 15)]
    public int WebRequestCacheMinutes { get; set; }

    [Setting(SettingScope.User, DefaultValue = 5)]
    public int SyncPlaybackCacheExpiry { get; set; }

    [Setting(SettingScope.User, DefaultValue = 1)]
    public int LogLevel { get; set; }

    [Setting(SettingScope.User)]
    public TraktLastSyncActivities LastSyncActivities { get; set; }

    [Setting(SettingScope.User)]
    public IEnumerable<TraktCache.ListActivity> LastListActivities { get; set; }

    [Setting(SettingScope.User, DefaultValue = true)]
    public bool SkipMoviesWithNoIdsOnSync { get; set; }

    [Setting(SettingScope.User, DefaultValue = 100)]
    public int SyncBatchSize { get; set; }

    #region Properties

    /// <summary>
    /// UserAgent used for Web Requests
    /// </summary>
    public string UserAgent
    {
      get
      {
        return string.Format("TraktForMP2/{0}", Version);
      }
      
    }

    /// <summary>
    /// Version of Plugin
    /// </summary>
    public string Version
    {
      get
      {
        return Assembly.GetCallingAssembly().GetName().Version.ToString();
      }
      
    }

    /// <summary>
    /// Build Date of Plugin
    /// </summary>
    public string BuildDate
    {
      get
      {
        if (_BuildDate == null)
        {
          const int PeHeaderOffset = 60;
          const int LinkerTimestampOffset = 8;

          byte[] buffer = new byte[2047];
          using (Stream stream = new FileStream(Assembly.GetAssembly(typeof(TraktSettings)).Location, FileMode.Open, FileAccess.Read))
          {
            stream.Read(buffer, 0, 2047);
          }

          int secondsSince1970 = BitConverter.ToInt32(buffer, BitConverter.ToInt32(buffer, PeHeaderOffset) + LinkerTimestampOffset);

          _BuildDate = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(secondsSince1970).ToString("yyyy-MM-dd");
        }
        return _BuildDate;
      }
    }
    private static string _BuildDate;

    #endregion
  }
}
