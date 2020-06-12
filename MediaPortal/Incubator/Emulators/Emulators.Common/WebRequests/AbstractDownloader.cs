using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.WebRequests
{
  public abstract class AbstractDownloader
  {
    protected Encoding _encoding = Encoding.Default;

    public Encoding Encoding
    {
      get { return _encoding; }
      set { _encoding = value; }
    }

    public virtual T Download<T>(string url, string cachePath = null)
    {
      try
      {
        string responseString;
        if (TryGetCache(cachePath, out responseString))
          return Deserialize<T>(responseString);

        responseString = GetResponseString(url);
        if (string.IsNullOrEmpty(responseString))
          return default(T);

        T response = Deserialize<T>(responseString);
        WriteCache(cachePath, responseString);
        return response;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Exception downloading from '{0}'", ex, url);
      }
      return default(T);
    }

    public virtual Task<bool> DownloadFileAsync(string url, string downloadFile)
    {
      return DownloadFileAsync(url, downloadFile, false);
    }

    public virtual async Task<bool> DownloadFileAsync(string url, string downloadFile, bool overwrite)
    {
      if (! overwrite && File.Exists(downloadFile))
        return true;
      try
      {
        WebClient webClient = new CompressionWebClient();
        webClient.Encoding = _encoding;
        await webClient.DownloadFileTaskAsync(url, downloadFile).ConfigureAwait(false);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Exception downloading file from '{0}' to '{1}'", ex, url, downloadFile);
        return false;
      }
    }

    protected abstract T Deserialize<T>(string response);

    protected virtual string GetResponseString(string url)
    {
      WebClient webClient = new CompressionWebClient();
      webClient.Encoding = _encoding;
      return webClient.DownloadString(url);
    }

    protected virtual bool TryGetCache(string cachePath, out string cacheString)
    {
      cacheString = null;
      if (string.IsNullOrEmpty(cachePath) || !File.Exists(cachePath))
        return false;
      cacheString = File.ReadAllText(cachePath);
      return true;
    }

    /// <summary>
    /// Writes XML strings to cache file.
    /// </summary>
    /// <param name="cachePath"></param>
    /// <param name="cacheString"></param>
    protected virtual void WriteCache(string cachePath, string cacheString)
    {
      if (string.IsNullOrEmpty(cachePath))
        return;

      using (FileStream fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
      {
        using (StreamWriter sw = new StreamWriter(fs))
        {
          sw.Write(cacheString);
          sw.Close();
        }
        fs.Close();
      }
    }
  }
}
