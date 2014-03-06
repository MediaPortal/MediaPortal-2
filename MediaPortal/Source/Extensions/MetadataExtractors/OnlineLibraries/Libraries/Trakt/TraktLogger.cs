using System;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt
{
  static class TraktLogger
  {
    static TraktLogger()
    {
      // listen to webclient events from the TraktAPI so we can provide useful logging            
      TraktAPI.OnDataSend += TraktAPI_OnDataSend;
      TraktAPI.OnDataError += TraktAPI_OnDataError;
      TraktAPI.OnDataReceived += TraktAPI_OnDataReceived;
    }

    internal static void Info(String log)
    {
      if (TraktSettings.LogLevel >= 2)
        ServiceRegistration.Get<ILogger>().Info(log);
    }

    internal static void Info(String format, params Object[] args)
    {
      Info(String.Format(format, args));
    }

    internal static void Debug(String log)
    {
      if (TraktSettings.LogLevel >= 3)
        ServiceRegistration.Get<ILogger>().Debug(log);
    }

    internal static void Debug(String format, params Object[] args)
    {
      Debug(String.Format(format, args));
    }

    internal static void Error(String log)
    {
      if (TraktSettings.LogLevel >= 0)
        ServiceRegistration.Get<ILogger>().Error(log);
    }

    internal static void Error(String format, params Object[] args)
    {
      Error(String.Format(format, args));
    }

    internal static void Warning(String log)
    {
      if (TraktSettings.LogLevel >= 1)
        ServiceRegistration.Get<ILogger>().Warn(log);
    }

    internal static void Warning(String format, params Object[] args)
    {
      Warning(String.Format(format, args));
    }

    private static void TraktAPI_OnDataSend(string address, string data)
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

    private static void TraktAPI_OnDataReceived(string response)
    {
      Debug("Response: {0}", response ?? "null");
    }

    private static void TraktAPI_OnDataError(string error)
    {
      Error("WebException: {0}", error);
    }

    /// <summary>
    /// Logs the result of Trakt api call
    /// </summary>
    /// <typeparam name="T">Response Type of message</typeparam>
    /// <param name="response">The response object holding the message to log</param>
    internal static bool LogTraktResponse<T>(T response)
    {
      try
      {
        TraktResponse traktResponse = (response as TraktResponse);
        if (traktResponse == null || traktResponse.Status == null)
        {
          // server is probably temporarily unavailable
          // return true even though it failed, so we can try again
          // currently the return value is only being used in livetv/recordings
          Error("Response from server was unexpected.");
          return true;
        }

        // check response error status
        if (traktResponse.Status != "success")
        {
          Error(traktResponse.Error == "The remote server returned an error: (401) Unauthorized." ? "401 Unauthorized, Please check your Username and Password" : traktResponse.Error);
          return false;
        }

        // success
        if (!string.IsNullOrEmpty(traktResponse.Message))
        {
          Info("Response: {0}", traktResponse.Message);
        }
        else
        {
          // no message returned on movie sync success
          TraktSyncResponse traktSyncResponse = (response as TraktSyncResponse);
          if (traktSyncResponse != null)
          {
            string message = "Response: Items Inserted: {0}, Items Already Exist: {1}, Items Skipped: {2}";
            Info(message, traktSyncResponse.Inserted, traktSyncResponse.AlreadyExist, traktSyncResponse.Skipped);
          }
        }
        return true;
      }
      catch (Exception)
      {
        Info("Response: {0}", "Failed to interpret response from server");
        return false;
      }
    }
  }
}
