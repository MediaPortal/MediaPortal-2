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
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Enums;

namespace MediaPortal.UiComponents.Trakt.Settings
{
  public class TraktSettings
  {
                          
    private const string APP_ID = "aea41e88de3cd0f8c8b2404d84d2e5d7317789e67fad223eba107aea2ef59068";
    private static Object lockObject = new object();

    [Setting(SettingScope.User)]
    public bool EnableTrakt { get; set; }

    [Setting(SettingScope.User, DefaultValue = "")]
    public string Username { get; set; }

    [Setting(SettingScope.User, DefaultValue = "")]
    public string Password { get; set; }

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
    public List<TraktAuthentication> UserLogins { get; set; }

    [Setting(SettingScope.User)]
    public TraktLastSyncActivities LastSyncActivities { get; set; }

    [Setting(SettingScope.User)]
    public IEnumerable<TraktCache.ListActivity> LastListActivities { get; set; }

    [Setting(SettingScope.User, DefaultValue = true)]
    public bool UseCompNameOnPassKey { get; set; }

    [Setting(SettingScope.User, DefaultValue = true)]
    public bool SkipMoviesWithNoIdsOnSync { get; set; }

    [Setting(SettingScope.User, DefaultValue = 100)]
    public int SyncBatchSize { get; set; }

    #region Properties

    public string ApplicationId => APP_ID;

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
    /// The current connection status to trakt.tv
    /// </summary>
    public ConnectionState AccountStatus
    {
      get
      {
        lock (lockObject)
        {
          if (_AccountStatus == ConnectionState.Pending)
          {
            // update state, to inform we are connecting now
            _AccountStatus = ConnectionState.Connecting;

          //  TraktLogger.Info("Logging into trakt.tv");

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
           //   TraktLogger.Info("Unable to login to trakt.tv, username and/or password is empty");
              return ConnectionState.Disconnected;
            }

            var response = TraktAPI.Login();
            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
              // set the user token for all future requests
              TraktAPI.UserToken = response.Token;

           //   TraktLogger.Info("User {0} successfully signed into trakt.tv", TraktSettings.Username);
              _AccountStatus = ConnectionState.Connected;

              if (!UserLogins.Exists(u => u.Username == Username))
              {
                UserLogins.Add(new TraktAuthentication { Username = Username, Password = Password });
              }
            }
            else
            {
              // check the error code for the type of error retured
              if (response != null && response.Description != null)
              {
             //   TraktLogger.Error("Login to trakt.tv failed, Code = '{0}', Reason = '{1}'", response.Code, response.Description);

                switch (response.Code)
                {
                  case 401:
                    _AccountStatus = ConnectionState.UnAuthorised;
                    break;

                  default:
                    _AccountStatus = ConnectionState.Invalid;
                    break;
                }
              }
              else
              {
                // very unlikely to ever hit this condition since we should get some sort of protocol error
                // if a problem with login or server error
                _AccountStatus = ConnectionState.Invalid;
              }
            }
          }
        }
        return _AccountStatus;
      }
      set
      {
        lock (lockObject)
        {
          _AccountStatus = value;
        }
      }
    }
    public static ConnectionState _AccountStatus = ConnectionState.Pending;

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
