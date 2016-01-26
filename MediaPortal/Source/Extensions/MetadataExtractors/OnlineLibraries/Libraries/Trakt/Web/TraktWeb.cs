using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Web
{

  public class ExtendedWebClient : WebClient
  {
    public int Timeout { private get; set; }

    protected override WebRequest GetWebRequest(Uri address)
    {
      WebRequest request = base.GetWebRequest(address);
      if (request != null)
        request.Timeout = Timeout;
      return request;
    }

    public ExtendedWebClient()
    {
      Timeout = 100000; // the standard HTTP Request Timeout default
    }
  }

  public static class TraktWeb
  {
    public static readonly Dictionary<string, string> CustomRequestHeaders = new Dictionary<string, string>();

    #region Events
    internal delegate void OnDataSendDelegate(string url, string postData);
    internal delegate void OnDataReceivedDelegate(string response);
    internal delegate void OnDataErrorReceivedDelegate(string error);

    internal static event OnDataSendDelegate OnDataSend;
    internal static event OnDataReceivedDelegate OnDataReceived;
    internal static event OnDataErrorReceivedDelegate OnDataErrorReceived;
    #endregion

    public static string GetFromTrakt(string address)
    {
      if (OnDataSend != null)
        OnDataSend(address, null);

      var request = WebRequest.Create(address) as HttpWebRequest;

      request.KeepAlive = true;
      request.Method = "GET";
      request.ContentLength = 0;
      request.Timeout = 120000;
      request.ContentType = "application/json";
      request.UserAgent = UserAgent;
      foreach (var header in CustomRequestHeaders)
      {
        request.Headers.Add(header.Key, header.Value);
      }

      try
      {
        WebResponse response = (HttpWebResponse)request.GetResponse();
        if (response == null) return null;

        Stream stream = response.GetResponseStream();
        StreamReader reader = new StreamReader(stream);
        string strResponse = reader.ReadToEnd();

        if (OnDataReceived != null)
          OnDataReceived(strResponse);

        stream.Close();
        reader.Close();
        response.Close();

        return strResponse;
      }
      catch (WebException e)
      {
        if (OnDataErrorReceived != null)
          OnDataErrorReceived(e.Message);

        return null;
      }
    }

    public static string PostToTrakt(string address, string postData, bool logRequest = true)
    {
      if (OnDataSend != null && logRequest)
        OnDataSend(address, postData);

      byte[] data = new UTF8Encoding().GetBytes(postData);

      var request = WebRequest.Create(address) as HttpWebRequest;
      request.KeepAlive = true;

      request.Method = "POST";
      request.ContentLength = data.Length;
      request.Timeout = 120000;
      request.ContentType = "application/json";
      request.UserAgent = UserAgent;
      foreach (var header in CustomRequestHeaders)
      {
        request.Headers.Add(header.Key, header.Value);
      }

      try
      {
        // post to trakt
        Stream postStream = request.GetRequestStream();
        postStream.Write(data, 0, data.Length);

        // get the response
        var response = (HttpWebResponse)request.GetResponse();
        if (response == null) return null;

        Stream responseStream = response.GetResponseStream();
        StreamReader reader = new StreamReader(responseStream);
        string strResponse = reader.ReadToEnd();

        if (OnDataReceived != null)
          OnDataReceived(strResponse);

        // cleanup
        postStream.Close();
        responseStream.Close();
        reader.Close();
        response.Close();

        return strResponse;
      }
      catch (WebException e)
      {
        if (OnDataErrorReceived != null)
          OnDataErrorReceived(e.Message);

        return null;
      }
    }

    public static string UserAgent
    {
      get
      {
        return string.Format("TraktForMP2/{0}", Version);
      }
    }

    private static string Version
    {
      get
      {
        return Assembly.GetCallingAssembly().GetName().Version.ToString();
      }
    }
  }
}
