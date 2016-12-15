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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Extension;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Web;
using MediaPortal.UiComponents.Trakt.Settings;

namespace MediaPortal.UiComponents.Trakt
{
  public static class TraktLogger
  {
    private static readonly TraktSettings SETTINGS = ServiceRegistration.Get<ISettingsManager>().Load<TraktSettings>();

    static TraktLogger()
    {

      // default logging before we load settings
      SETTINGS.LogLevel = 2;

      // listen to webclient events from the TraktWeb so we can provide useful logging            
      TraktWeb.OnDataSend += WebRequest_OnDataSend;
      TraktWeb.OnDataReceived += WebRequest_OnDataReceived;
      TraktWeb.OnDataErrorReceived += WebRequest_OnDataErrorReceived;
    }

    public static void Info(String log)
    {
      if (SETTINGS.LogLevel >= 2)
        ServiceRegistration.Get<ILogger>().Info("Trakt.tv: {0}", log);
    }

    public static void Info(String format, params Object[] args)
    {
      Info(String.Format(format, args));
    }

    public static void Debug(String log)
    {
      if (SETTINGS.LogLevel >= 3)
        ServiceRegistration.Get<ILogger>().Debug("Trakt.tv: {0}", log);
    }

    public static void Debug(String format, params Object[] args)
    {
      Debug(String.Format(format, args));
    }

    public static void Error(String log)
    {
      if (SETTINGS.LogLevel >= 0)
        ServiceRegistration.Get<ILogger>().Error("Trakt.tv: {0}", log);
    }

    public static void Error(String format, params Object[] args)
    {
      Error(String.Format(format, args));
    }

    public static void Warning(String log)
    {
      if (SETTINGS.LogLevel >= 1)
        ServiceRegistration.Get<ILogger>().Warn("Trakt.tv: {0}", log);
    }

    public static void Warning(String format, params Object[] args)
    {
      Warning(String.Format(format, args));
    }

    private static void WebRequest_OnDataSend(string address, string data)
    {
      if (!string.IsNullOrEmpty(data))
      {
        Debug("Address: {0}, Post: {1}", address, data);
      }
      else
      {
        Debug("Address: {0}", address);
      }
    }

    private static void WebRequest_OnDataReceived(string response)
    {
      if (SETTINGS.LogLevel >= 3)
      {
        Debug("Response: {0}", response ?? "null");
      }
    }

    private static void WebRequest_OnDataErrorReceived(string error)
    {
      Error("Response: {0}", error ?? "null");
    }

    /// <summary>
    /// Logs the result of Trakt api call
    /// </summary>
    /// <typeparam name="T">Response Type of message</typeparam>
    /// <param name="response">The response object holding the message to log</param>
    public static bool LogTraktResponse<T>(T response)
    {
      if (response == null)
      {
        // we already log errors which would normally not be able to be deserialised
        // currently the return value is only being used in livetv/recordings
        return true;
      }

      try
      {
        // only log the response if we don't have debug logging enabled
        // we already log all responses in debug level
        if (SETTINGS.LogLevel < 3)
        {
          if ((response is TraktSyncResponse))
          {
            Info("Sync Response: {0}", (response as TraktSyncResponse).ToJSON());
          }
          else if ((response is TraktScrobbleResponse))
          {
            // status code will be greater than 0 if we caught an error
            // we already log errors so we can supress the scrobble log result
            var scrobbleResponse = response as TraktScrobbleResponse;
            if (scrobbleResponse != null && scrobbleResponse.Code == 0)
            {
              Info("Scrobble Response: {0}", scrobbleResponse.ToJSON());
            }
          }
        }

        return true;
      }
      catch (Exception)
      {
        Info("Response: Failed to interpret response from server");
        return false;
      }
    }
  }
}
