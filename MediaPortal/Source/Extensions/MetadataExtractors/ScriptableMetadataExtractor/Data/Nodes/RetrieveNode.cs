#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes
{
  [ScraperNode("retrieve")]
  public class RetrieveNode : ScraperNode
  {
    #region Properties

    public string Url { get; protected set; }
    public string File { get; protected set; }
    public int MaxRetries { get; protected set; }
    public Encoding Encoding { get; protected set; }
    public string UserAgent { get; protected set; }
    public int Timeout { get; protected set; }
    public int TimeoutIncrement { get; protected set; }
    public bool AllowUnsafeHeader { get; protected set; }
    public bool UseCaching { get; protected set; }
    public string Cookies { get; protected set; }
    public string AcceptLanguage { get; protected set; }
    public string Method { get; protected set; }

    #endregion

    #region Methods

    public RetrieveNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {
      // Set default attribute values
      UseCaching = true;
      AllowUnsafeHeader = false;
      MaxRetries = 5;
      Timeout = 5000;
      TimeoutIncrement = 2000;
      Method = "GET";
      AcceptLanguage = context.Language;

      // Load attributes
      foreach (XmlAttribute attr in xmlNode.Attributes)
      {
        switch (attr.Name)
        {
          case "url":
            Url = attr.Value;
            break;
          case "file":
            File = attr.Value;
            break;
          case "useragent":
            UserAgent = attr.Value;
            break;
          case "accept_language":
            AcceptLanguage = attr.Value;
            break;
          case "allow_unsafe_header":
            if (bool.TryParse(attr.Value, out var allow))
              AllowUnsafeHeader = allow;
            break;
          case "use_caching":
            if (bool.TryParse(attr.Value, out var cache))
              UseCaching = cache;
            break;
          case "encoding":
            // grab encoding, if not specified it will try to set 
            // the encoding using information from the response header.
            try
            {
              Encoding = Encoding.GetEncoding(attr.Value);
            }
            catch
            {
            }
            break;
          case "retries":
            if (int.TryParse(attr.Value, out var retries))
              MaxRetries = retries;
            break;
          case "timeout":
            if (int.TryParse(attr.Value, out var timeout))
              Timeout = timeout;
            break;
          case "timeout_increment":
            if (int.TryParse(attr.Value, out var timeoutIncrement))
              TimeoutIncrement = timeoutIncrement;
            break;
          case "cookies":
            Cookies = attr.Value;
            break;
          case "method":
            Method = attr.Value.Trim().ToUpper();
            break;
        }
      }

      // Validate URL / FILE attribute
      if (Url == null && File == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing URL or FILE attribute on: {0}", xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing retrieve: " + xmlNode.OuterXml);

      // Check for calling class provided useragent
      if (UserAgent == null && variables.ContainsKey("settings.defaultuseragent"))
        UserAgent = variables["settings.defaultuseragent"];

      if (UserAgent == null)
        UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.95 Safari/537.11";

      string parsedName = ParseString(variables, Name);
      string stringData = string.Empty;

      if (Url != null)
        stringData = RetrieveUrl(variables);
      else
        stringData = ReadFile(variables);

      // Set variable
      if (stringData != null)
      {
        SetVariable(variables, parsedName, stringData);
      }
    }

    // Retrieves an URL
    private string RetrieveUrl(Dictionary<string, string> variables)
    {
      string parsedUrl = ParseString(variables, Url);
      string parsedUserAgent = ParseString(variables, UserAgent);
      string pageContents = string.Empty;

      if (UseCaching && Context.Cache.ContainsKey(parsedUrl))
      {
        Logger.Debug("ScriptableScraperProvider: Using Cached Version of URL: {0}", parsedUrl);
        return Context.Cache[parsedUrl];
      }

      Logger.Debug("ScriptableScraperProvider: Retrieving URL: {0}", parsedUrl);

      // Try to grab the document
      try
      {
        WebGrabber grabber = new WebGrabber(parsedUrl);
        grabber.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        grabber.UserAgent = parsedUserAgent;
        grabber.Encoding = Encoding;
        grabber.Timeout = Timeout;
        grabber.TimeoutIncrement = TimeoutIncrement;
        grabber.MaxRetries = MaxRetries;
        grabber.AllowUnsafeHeader = AllowUnsafeHeader;
        grabber.CookieHeader = Cookies;
        grabber.AcceptLanguage = AcceptLanguage;

        // Keep session / chaining
        string sessionKey = "urn://scraper/header/" + grabber.Request.RequestUri.Host;
        if (variables.ContainsKey(sessionKey))
        {
          if (grabber.CookieHeader == null)
            grabber.CookieHeader = variables[sessionKey];
          else
            grabber.CookieHeader = grabber.CookieHeader + "," + variables[sessionKey];
        }

        // Retrieve the document
        if (grabber.GetResponse())
        {
          // save cookie session data for future requests
          SetVariable(variables, sessionKey, grabber.CookieHeader);

          // grab the request results and store in our cache for later retrievals
          pageContents = grabber.GetString();
          if (UseCaching) Context.Cache[parsedUrl] = pageContents;
        }
      }
      catch (Exception e)
      {
        Logger.Warn("ScriptableScraperProvider: Could not connect to {0}", parsedUrl, e);
      }

      return pageContents;
    }

    // Reads a file
    private string ReadFile(Dictionary<string, string> variables)
    {
      string parsedFile = ParseString(variables, File);
      string fileContents = string.Empty;

      if (System.IO.File.Exists(parsedFile))
      {
        Logger.Debug("ScriptableScraperProvider: Reading file: {0}", parsedFile);

        try
        {
          StreamReader streamReader;
          if (Encoding != null)
            streamReader = new StreamReader(parsedFile, Encoding);
          else
            streamReader = new StreamReader(parsedFile);

          fileContents = streamReader.ReadToEnd();
          streamReader.Close();
        }
        catch (Exception e)
        {
          Logger.Warn("ScriptableScraperProvider: Could not read file: {0}", parsedFile, e);
        }
      }
      else
      {
        Logger.Debug("ScriptableScraperProvider: File does not exist: {0}", parsedFile);
      }

      return fileContents;
    }
    #endregion
  }
}
