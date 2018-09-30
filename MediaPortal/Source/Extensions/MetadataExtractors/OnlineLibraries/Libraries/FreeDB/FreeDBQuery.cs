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

using MediaPortal.Extensions.OnlineLibraries.Libraries.Freedb.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Freedb
{
  /// <summary>
  /// This Class Establishes a connection to a FreeDB Site and retrieves the information for the Audio CD inserted
  /// </summary>
  public class FreeDBQuery 
  {
    const string APPNAME="MediaPortalII";
    const string APPVERSION="1.0";
    private FreeDBSite m_server = null;
    private string m_serverURL = null;
    private string m_idStr = null;
    private string m_message = null;
    private int m_code = 0;

    public FreeDBQuery()
    {
      StringBuilder buff = new StringBuilder(512);
      buff.Append("&hello=");
      buff.Append(Environment.UserName.Replace(" ", "_"));
      buff.Append('+');
      buff.Append(Environment.MachineName);
      buff.Append('+');
      buff.Append(APPNAME);
      buff.Append('+');
      buff.Append(APPVERSION);
      buff.Append("&proto=6");
      m_idStr =  buff.ToString();
    }

    public bool Connect()
    {
      m_server = new FreeDBSite("freedb.freedb.org", FreeDBSite.FreeDBProtocol.HTTP, 80, "/~cddb/cddb.cgi",
                                "N000.00", "W000.00", "Random freedb server");

      m_serverURL = "http://"+m_server.Host+":"+m_server.Port+m_server.URI;

      return true;
    }

    public bool Connect(FreeDBSite site)
    {
      m_server = site;
      m_serverURL = "http://"+m_server.Host+":"+m_server.Port+m_server.URI;
      return true;
    }

    public bool Disconnect()
    {
      return true;
    }

    public async Task<FreeDBSite[]> GetFreedbSitesAsync()
    {
      FreeDBSite[] retval = null;
      // FIXME: Close reader
      StreamReader urlRdr = await GetStreamFromSiteAsync("sites").ConfigureAwait(false);
      m_message = await urlRdr.ReadLineAsync().ConfigureAwait(false);
      int code = GetCode(m_message);
      m_message = m_message.Substring(4);  // remove the code...
      char[] sep = {' '};

      switch(code)
      {
        case 210: // OK, Site Information Follows.
          // Read in all sites.
          string[] sites = await ParseMultiLineAsync(urlRdr).ConfigureAwait(false);
          retval = new FreeDBSite[sites.Length];
          int index =0;
          // Loop through server list and extract different parts.
          foreach (string site in sites)
          {
            string loc = "";
            string[] siteInfo = site.Split(sep);
            retval[index] = new FreeDBSite();
            retval[index].Host = siteInfo[0];
            retval[index].Protocol = (FreeDBSite.FreeDBProtocol) Enum.Parse(typeof(FreeDBSite.FreeDBProtocol), siteInfo[1], true);
            retval[index].Port = Convert.ToInt32(siteInfo[2]);
            retval[index].URI = siteInfo[3];
            retval[index].Latitude = siteInfo[4];
            retval[index].Longitude = siteInfo[5];

            for(int i = 6; i < siteInfo.Length; i++)
              loc += retval[i]+" ";
            retval[index].Location = loc;
            index++;
          }
          break;
        case 401: // No Site Information Available.
          break;
        default:
          break;
      }
      return retval;
    }

    public string GetServerMessage()
    {
      return m_message;
    }

    public Task<string[]> GetListOfGenresAsync()
    {
      return GetInfoAsync("cddb+lscat");
    }

    public Task<string[]> GetHelpAsync(string topic)
    {
      return GetInfoAsync("help " + topic);
    }

    public Task<string[]> GetLogAsync()
    {
      return GetInfoAsync("log");
    }

    public Task<string[]> GetMessageOfTheDayAsync()
    {
      return GetInfoAsync("motd");
    }

    public Task<string[]> GetStatusAsync()
    {
      return GetInfoAsync("stat");
    }

    public Task<string[]> GetUsersAsync()
    {
      return GetInfoAsync("whom");
    }

    public async Task<string> GetVersionAsync()
    {
      await GetInfoAsync("ver", false).ConfigureAwait(false);
      return GetServerMessage();
    }

    public async Task<FreeDBCDInfoDetail> GetDiscDetailsAsync(string category, string discid)
    {
      string[] content = await GetInfoAsync("cddb+read+" + category + "+" + discid).ConfigureAwait(false);
      XMCDParser parser = new XMCDParser();
      FreeDBCDInfoDetail cdInfo = parser.Parse2(content);
      return cdInfo;
    }

    public Task<string[]> GetDiscDetailsXMCDAsync(string category, string discid)
    {
      return GetInfoAsync("cddb+read+" + category + "+" + discid);
    }

    public FreeDBCDInfoDetail GetDiscDetailsFromXMCD(string[] xmcd)
    {
      XMCDParser parser = new XMCDParser();
      FreeDBCDInfoDetail cdInfo = parser.Parse2(xmcd);
      return cdInfo;
    }

    public async Task<Dictionary<string, string[]>> GetDiscDetailsXMCDFromIdAsync(string discid)
    {
      //Getting disc is not possible with only disc id
      //Category is needed too
      Dictionary<string, string[]> retValue = new Dictionary<string, string[]>();
      string[] genres = await GetListOfGenresAsync().ConfigureAwait(false);
      foreach (string genre in genres)
      {
        string[] xmcd = await GetDiscDetailsXMCDAsync(genre, discid).ConfigureAwait(false);
        if (xmcd != null) retValue.Add(genre, xmcd);
      }
      return retValue;
    }

    public async Task<FreeDBCDInfo[]> GetDiscInfoByQueryAsync(string cddbQuery)
    {
      FreeDBCDInfo[] retval = null;
      string command = "cddb+query+" + cddbQuery.Replace(" ", "+");
      StreamReader urlRdr = await GetStreamFromSiteAsync(command).ConfigureAwait(false);
      try
      {
        m_message = await urlRdr.ReadLineAsync().ConfigureAwait(false);
        int code = GetCode(m_message);
        m_message = m_message.Substring(4);  // remove the code...

        char[] sep = { ' ' };
        string title = "";
        int index = 0;
        string[] match;
        string[] matches;

        switch (code)
        {
          case 200: // Exact Match...
            match = m_message.Split(sep);
            retval = new FreeDBCDInfo[1];

            retval[0] = new FreeDBCDInfo();
            retval[0].Category = match[0];
            retval[0].DiscId = match[1];
            for (int i = 2; i < match.Length; i++)
              title += match[i] + " ";
            retval[0].Title = title.Trim();
            break;
          case 202: // no match found
            break;
          case 211: // Found Inexact Matches. List Follows.
          case 210: // Found Exact Matches. List Follows.
            matches = await ParseMultiLineAsync(urlRdr).ConfigureAwait(false);
            retval = new FreeDBCDInfo[matches.Length];
            foreach (string line in matches)
            {
              match = line.Split(sep);

              retval[index] = new FreeDBCDInfo();
              retval[index].Category = match[0];
              retval[index].DiscId = match[1];
              for (int i = 2; i < match.Length; i++)
                title += match[i] + " ";
              retval[index].Title = title.Trim();
              index++;
            }
            break;
          case 403: // Database Entry is Corrupt.
            retval = null;
            break;
          case 409: // No handshake... Should not happen!
            retval = null;
            break;
          default:
            retval = null;
            break;
        }
      }
      finally
      {
        urlRdr.Close();
      }
      return retval;
    }

    private Task<string[]> GetInfoAsync(string command)
    {
      return GetInfoAsync(command, true);
    }

    private async Task<string[]> GetInfoAsync(string command, bool multipleLine)
    {
      string[] retval = null;
      StreamReader urlRdr = await GetStreamFromSiteAsync(command).ConfigureAwait(false);
      try
      {
        m_message = urlRdr.ReadLine();
        int code = GetCode(m_message);
        m_message = m_message.Substring(4);  // remove the code...

        switch (code / 100)
        {
          case 2: // no problem
            retval = await ParseMultiLineAsync(urlRdr).ConfigureAwait(false);
            break;
          case 4: // no permission
            retval = null;
            break;
          case 5: // problem
            retval = null;
            break;
          default:
            retval = null;
            break;
        }
      }
      finally
      {
        urlRdr.Close();
      }
      return retval;
    }

    private async Task<StreamReader> GetStreamFromSiteAsync(string command)
    {
      System.Uri url = new System.Uri(m_serverURL + "?cmd=" + command +  m_idStr);
      WebRequest req = WebRequest.Create(url);
      WebResponse resp = await req.GetResponseAsync().ConfigureAwait(false);
      StreamReader urlRdr = new StreamReader(new StreamReader(resp.GetResponseStream()).BaseStream, Encoding.UTF8);

      return urlRdr;
    }

    private int GetCode(string content)
    {
      m_code = Convert.ToInt32(content.Substring(0,3));
      return m_code;
    }

    private async Task<string> ParseSingleLineAsync(StreamReader streamReader)
    {
      return (await streamReader.ReadLineAsync().ConfigureAwait(false)).Trim();
    }

    private async Task<string[]> ParseMultiLineAsync(StreamReader streamReader)
    {
      ArrayList strarray = new ArrayList();
      string curLine;

      while ((curLine = await streamReader.ReadLineAsync().ConfigureAwait(false)) != null) 
      {
        curLine = curLine.Trim();
        if(curLine.Trim().Length > 0 && !curLine.Trim().Equals("."))
          strarray.Add(curLine);
      }
      return (string[])strarray.ToArray(typeof(string));
    }
  }
}
