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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data
{
  public class WebGrabber
  {

    #region Private variables

    private static ILogger Logger => ServiceRegistration.Get<ILogger>();
    private static int unsafeHeaderUserCount;
    private static object lockingObj;
    private string requestUrl;

    #endregion

    #region Ctor

    static WebGrabber()
    {
      unsafeHeaderUserCount = 0;
      lockingObj = new object();
    }

    public WebGrabber(string url)
    {
      requestUrl = url;
      Request = (HttpWebRequest)WebRequest.Create(requestUrl);
    }

    public WebGrabber(Uri uri)
    {
      requestUrl = uri.OriginalString;
      Request = (HttpWebRequest)WebRequest.Create(uri);
    }

    ~WebGrabber()
    {
      Request = null;
      if (Response != null)
      {
        Response.Close();
        Response = null;
      }
    }

    #endregion

    #region Public properties

    public HttpWebRequest Request { get; private set; }
    public HttpWebResponse Response { get; private set; }
    public Encoding Encoding { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int Timeout { get; set; } = 5000;
    public int TimeoutIncrement { get; set; } = 1000;
    public string UserAgent { get; set; }
    public string CookieHeader { get; set; }
    public string Method { get; set; } = "GET";
    public bool AllowUnsafeHeader { get; set; }
    public string Accept { get; set; }
    public string AcceptLanguage { get; set; }

    #endregion

    #region Public methods

    public bool GetResponse()
    {
      try
      {
        bool completed = false;
        int tryCount = 0;

        // enable unsafe header parsing if needed
        if (AllowUnsafeHeader) SetAllowUnsafeHeaderParsing(true);

        // In .NET 4.0 default transport level security standard is TLS 1.1,
        // Some endpoints will reject this older insecure standard.
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

        // setup some request properties
        Request.Proxy = WebRequest.DefaultWebProxy;
        Request.Proxy.Credentials = CredentialCache.DefaultCredentials;
        if (UserAgent != null)
          Request.UserAgent = UserAgent;
        Request.Method = Method;
        Request.Accept = Accept;
        Request.Headers["Accept-Encoding"] = "gzip, deflate";
        Request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        if (AcceptLanguage != null)
        {
          Request.Headers["Accept-Language"] = AcceptLanguage;
        }
        Request.CookieContainer = new CookieContainer();

        while (!completed)
        {
          tryCount++;

          Request.Timeout = Timeout + (TimeoutIncrement * tryCount);
          if (CookieHeader != null)
            Request.CookieContainer.SetCookies(Request.RequestUri, CookieHeader.Replace(';', ','));

          try
          {
            Response = (HttpWebResponse)Request.GetResponse();
            completed = true;
          }
          catch (WebException e)
          {
            // Skip retry logic on protocol errors
            if (e.Status == WebExceptionStatus.ProtocolError)
            {
              HttpStatusCode statusCode = ((HttpWebResponse)e.Response).StatusCode;
              switch (statusCode)
              {
                // Currently the only exception is the service temporarily unavailable status
                // So keep retrying when this is the case
                case HttpStatusCode.ServiceUnavailable:
                  break;
                // all other status codes mostly indicate problems that won't be
                // solved within the retry period so fail these immediately
                default:
                  Logger.Error("ScriptableScraperProvider: Connection failed: URL={0}, Status={1}, Description={2}.", requestUrl, statusCode, ((HttpWebResponse)e.Response).StatusDescription);
                  return false;
              }
            }

            // Return when hitting maximum retries.
            if (tryCount == MaxRetries)
            {
              Logger.Warn("ScriptableScraperProvider: Connection failed: Reached retry limit of " + MaxRetries + ". URL=" + requestUrl);
              return false;
            }

            // If we did not experience a timeout but some other error
            // use the timeout value as a pause between retries
            if (e.Status != WebExceptionStatus.Timeout)
            {
              Thread.Sleep(Timeout + (TimeoutIncrement * tryCount));
            }
          }
          catch (NotSupportedException e)
          {
            Logger.Error("ScriptableScraperProvider: Connection failed.", e);
            return false;
          }
          catch (ProtocolViolationException e)
          {
            Logger.Error("ScriptableScraperProvider: Connection failed.", e);
            return false;
          }
          catch (InvalidOperationException e)
          {
            Logger.Error("ScriptableScraperProvider: Connection failed.", e);
            return false;
          }
          finally
          {
            // disable unsafe header parsing if it was enabled
            if (AllowUnsafeHeader) SetAllowUnsafeHeaderParsing(false);
          }
        }

        // persist the cookie header
        CookieHeader = Request.CookieContainer.GetCookieHeader(Request.RequestUri);

        // Debug
        Logger.Debug("ScriptableScraperProvider: GetResponse: URL={0}, UserAgent={1}, CookieHeader={2}, Accept={3}", requestUrl, UserAgent, CookieHeader, Accept);

        // disable unsafe header parsing if it was enabled
        if (AllowUnsafeHeader) SetAllowUnsafeHeaderParsing(false);

        return true;
      }
      catch (Exception e)
      {
        Logger.Warn("ScriptableScraperProvider: Unexpected error getting http response from '{0}'", e, requestUrl);
        return false;
      }
    }

    public string GetString()
    {
      if (Response == null)
        return null;

      // If encoding was not set manually try to detect it
      if (Encoding == null)
      {
        try
        {
          // Try to get the encoding using the character set
          Encoding = Encoding.GetEncoding(Response.CharacterSet);
        }
        catch (Exception e)
        {
          // If this fails default to the system's default encoding
          Logger.Debug("ScriptableScraperProvider: Encoding could not be determined, using default.", e);
          Encoding = Encoding.Default;
        }
      }

      // Debug
      Logger.Debug("ScriptableScraperProvider: GetString: Encoding={2}", Encoding.EncodingName);

      // Converts the stream to a string
      try
      {
        Stream stream = Response.GetResponseStream();
        StreamReader reader = new StreamReader(stream, Encoding, true);
        string data = reader.ReadToEnd();
        reader.Close();
        stream.Close();
        Response.Close();

        // return the string data
        return data;
      }
      catch (Exception e)
      {
        // There was an error reading the stream
        // TODO: might have to retry
        Logger.Error("ScriptableScraperProvider: Error while trying to read stream data: ", e);
      }

      // return nothing.
      return null;
    }

    public XmlNodeList GetXML()
    {
      return GetXML(null);
    }

    public XmlNodeList GetXML(string rootNode)
    {
      string data = GetString();

      // if there's no data return nothing
      if (string.IsNullOrEmpty(data))
        return null;

      XmlDocument xml = new XmlDocument();

      // attempts to convert data into an XmlDocument
      try
      {
        xml.LoadXml(data);
      }
      catch (XmlException e)
      {
        Logger.Error("ScriptableScraperProvider: XML Parse error: URL=" + requestUrl, e);
        return null;
      }

      // get the document root
      XmlElement xmlRoot = xml.DocumentElement;
      if (xmlRoot == null)
        return null;

      // if a root node name is given check for it
      // return null when the root name doesn't match
      if (rootNode != null && xmlRoot.Name != rootNode)
        return null;

      // return the node list
      return xmlRoot.ChildNodes;
    }

    #endregion

    #region Private methods

    //Method to change the AllowUnsafeHeaderParsing property of HttpWebRequest.
    private bool SetAllowUnsafeHeaderParsing(bool setState)
    {
      try
      {
        lock (lockingObj)
        {
          // update our counter of the number of requests needing 
          // unsafe header processing
          if (setState == true) unsafeHeaderUserCount++;
          else unsafeHeaderUserCount--;

          // if there was already a request using unsafe heaser processing, we
          // dont need to take any action.
          if (unsafeHeaderUserCount > 1)
            return true;

          // if the request tried to turn off unsafe header processing but it is
          // still needed by another request, we should wait.
          if (unsafeHeaderUserCount >= 1 && setState == false)
            return true;

          //Get the assembly that contains the internal class
          Assembly aNetAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
          if (aNetAssembly == null)
            return false;

          //Use the assembly in order to get the internal type for the internal class
          Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
          if (aSettingsType == null)
            return false;

          //Use the internal static property to get an instance of the internal settings class.
          //If the static instance isn't created already the property will create it for us.
          object anInstance = aSettingsType.InvokeMember("Section",
                                                          BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic,
                                                          null, null, new object[] { });
          if (anInstance == null)
            return false;

          //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
          FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
          if (aUseUnsafeHeaderParsing == null)
            return false;

          // and finally set our setting
          aUseUnsafeHeaderParsing.SetValue(anInstance, setState);
          return true;
        }

      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: Unsafe header parsing setting change failed.", e);
        return false;
      }
    }

    #endregion
  }
}
