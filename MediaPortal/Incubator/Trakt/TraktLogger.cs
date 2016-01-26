using System;
using System.Linq;
using System.Net;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Extension;
using MediaPortal.UiComponents.Trakt.Settings;

namespace MediaPortal.UiComponents.Trakt
{
  public static class TraktLogger
  {

    internal delegate void OnLogReceivedDelegate(string message, bool error);

    internal static event OnLogReceivedDelegate OnLogReceived;
    private static TraktSettings TRAKT_SETTINGS = ServiceRegistration.Get<ISettingsManager>().Load<TraktSettings>();

    static TraktLogger()
    {

      // default logging before we load settings
      TRAKT_SETTINGS.LogLevel = 2;

      // listen to webclient events from the TraktAPI so we can provide useful logging            
      TraktAPI.OnDataSend += new TraktAPI.OnDataSendDelegate(TraktAPI_OnDataSend);
      TraktAPI.OnDataError += new TraktAPI.OnDataErrorDelegate(TraktAPI_OnDataError);
      TraktAPI.OnDataReceived += new TraktAPI.OnDataReceivedDelegate(TraktAPI_OnDataReceived);
      TraktAPI.OnLatency += new TraktAPI.OnLatencyDelegate(TraktAPI_OnLatency);
    }

    public static void Info(String log)
    {
      if (TRAKT_SETTINGS.LogLevel >= 2)
        ServiceRegistration.Get<ILogger>().Info("Trakt.tv: {0}", log);
    }

    public static void Info(String format, params Object[] args)
    {
      Info(String.Format(format, args));
    }

    public static void Debug(String log)
    {
      if (TRAKT_SETTINGS.LogLevel >= 3)
        ServiceRegistration.Get<ILogger>().Info("Trakt.tv: {0}", log);
    }

    public static void Debug(String format, params Object[] args)
    {
      Debug(String.Format(format, args));
    }

    public static void Error(String log)
    {
      if (TRAKT_SETTINGS.LogLevel >= 0)
        ServiceRegistration.Get<ILogger>().Info("Trakt.tv: {0}", log);
    }

    public static void Error(String format, params Object[] args)
    {
      Error(String.Format(format, args));
    }

    public static void Warning(String log)
    {
      if (TRAKT_SETTINGS.LogLevel >= 1)
        ServiceRegistration.Get<ILogger>().Info("Trakt.tv: {0}", log);
    }

    public static void Warning(String format, params Object[] args)
    {
      Warning(String.Format(format, args));
    }

    private static String CreatePrefix()
    {
      return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [{0}] " + String.Format("[{0}][{1}]", Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(2, '0')) + ": {1}";
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

    private static void TraktAPI_OnDataReceived(string response, HttpWebResponse webResponse)
    {
      if (TRAKT_SETTINGS.LogLevel >= 3)
      {
        string headers = string.Empty;
        foreach (string key in webResponse.Headers.AllKeys)
        {
          headers += string.Format("{0}: {1}, ", key, webResponse.Headers[key]);
        }

        Debug("Response: {0}, Headers: {{{1}}}", response ?? "null", headers.TrimEnd(new char[] { ',', ' ' }));
      }
    }

    private static void TraktAPI_OnDataError(string error)
    {
      Error(error);
    }

    private static void TraktAPI_OnLatency(double totalTimeTaken, HttpWebResponse webResponse, int dataSent, int dataReceived)
    {
      double serverRuntime = 0.0;
      string[] headers = webResponse.Headers.AllKeys;
      if (headers.Contains("X-Runtime"))
      {
        double.TryParse(webResponse.Headers["X-Runtime"], out serverRuntime);

        // convert to milliseconds from seconds
        serverRuntime *= 1000.0;
      }

      // escape query string as it contains comma's
      string query = webResponse.ResponseUri.Query;
      if (!string.IsNullOrEmpty(query) && query.Contains(','))
      {
        query = "\"" + query + "\"";
      }

      //WriteLatency(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", DateTime.UtcNow.ToISO8601(), webResponse.ResponseUri.AbsolutePath, query, webResponse.Method, (int)webResponse.StatusCode, webResponse.StatusDescription, dataSent, dataReceived, serverRuntime, totalTimeTaken));
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
        if (TRAKT_SETTINGS.LogLevel < 3)
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
