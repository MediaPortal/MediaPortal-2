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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Web;
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UPnPRenderer.MediaItems;
using Microsoft.Win32;

namespace MediaPortal.UPnPRenderer.UPnP
{
  public enum ContentType
  {
    Image,
    Video,
    Audio,
    Unknown
  }

  class Utils
  {
    // UrlSourceFilter
    public const string FilterCLSID = "59ED045A-A938-4A09-A8A6-8231F5834259";
    public const string FilterName = "MediaPortal Url Source Splitter";

    [DllImport(@"urlmon.dll", CharSet = CharSet.Auto)]
    private static extern UInt32 FindMimeFromData(
      UInt32 pBC,
      [MarshalAs(UnmanagedType.LPStr)] String pwzUrl,
      [MarshalAs(UnmanagedType.LPArray)] byte[] pBuffer,
      UInt32 cbSize,
      [MarshalAs(UnmanagedType.LPStr)] String pwzMimeProposed,
      UInt32 dwMimeFlags,
      out UInt32 ppwzMimeOut,
      UInt32 dwReserverd
      );

    public static string GetMimeFromUrl(string url, string metaData = null)
    {
      // if we have metaData, use that!
      if (metaData != null)
      {
        using (XmlReader reader = XmlReader.Create(new StringReader(metaData)))
        {
          reader.ReadToFollowing("upnp:class");
          string output = reader.ReadElementContentAsString();

          if (output.IndexOf("videoItem", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            Logger.Debug("found mime from MetaData: Video");
            return "video/*";
          }
          if (output.IndexOf("audioItem", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            Logger.Debug("found mime from MetaData: Audio");
            return "audio/*";
          }
          if (output.IndexOf("imageItem", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            Logger.Debug("found mime from MetaData: Image");
            return "image/*";
          }
          Logger.Debug("couldn't find mime from MetaData");
        }
      }

      //we don't have any meta Data or couldn't find something

      WebRequest request = WebRequest.Create(url) as HttpWebRequest;

      byte[] buffer = new byte[256];

      // Try to get the mime type from the registry, works only if the server sends a file extension
      Uri uri = new Uri(url);
      string fileName = uri.Segments.Last();
      var mime = GetMimeFromRegistry(fileName);
      TraceLogger.WriteLine("Mime from registry: " + GetMimeFromRegistry(fileName));

      if (mime == "application/octet-stream")
      {
        using (WebResponse response = request.GetResponse())
        {
          using (Stream stream = response.GetResponseStream())
          {
            int count = stream.Read(buffer, 0, 256);

            TraceLogger.WriteLine("Bufer: " + BitConverter.ToString(buffer));
            TraceLogger.WriteLine(response.ContentType);
            TraceLogger.WriteLine("Sytem Mimemapping" + MimeMapping.GetMimeMapping(url));

            try
            {
              UInt32 mimetype;
              FindMimeFromData(0, null, buffer, 256, null, 0, out mimetype, 0);
              IntPtr mimeTypePtr = new IntPtr(mimetype);
              mime = Marshal.PtrToStringUni(mimeTypePtr);
              Marshal.FreeCoTaskMem(mimeTypePtr);

              TraceLogger.WriteLine("MimeType from urlmon.dll: " + mime);

              // if we get application/octet-stream => unknown mime type
              if (mime == "application/octet-stream")
              {
                TraceLogger.WriteLine("urlmon.dll couldn't find mime type");
                mime = response.ContentType;
                TraceLogger.WriteLine("MimeType from response.ContentType: " + mime);
                if (mime == "application/octet-stream")
                {
                  TraceLogger.WriteLine("response.ContentType couldn't find mime type");
                  mime = MimeMapping.GetMimeMapping(url);
                  TraceLogger.WriteLine("MimeType from GetMimeMapping: " + mime);

                  if (mime == "application/octet-stream")
                  {
                    throw new Exception("no mime type found");
                  }
                }
              }
              return mime;
            }
            catch (Exception e)
            {
              return "unknown/unknown";
            }
          }
        }
      }
      else
      {
        return mime;
      }
    }

    public static ContentType GetContentTypeFromUrl(string url, string metaData = null)
    {
      string mimeType = GetMimeFromUrl(url, metaData);
      if (mimeType.Contains("video"))
      {
        return ContentType.Video;
      }

      if (mimeType.Contains("image"))
      {
        return ContentType.Image;
      }

      if (mimeType.Contains("audio"))
      {
        return ContentType.Audio;
      }

      return ContentType.Unknown;
    }

    public static byte[] DownloadImage(string url)
    {
      byte[] buffer = { };
      try
      {
        WebRequest request = WebRequest.Create(url) as HttpWebRequest;

        using (WebResponse response = request.GetResponse())
        {
          using (Stream stream = response.GetResponseStream())
          {
            buffer = ReadFullStream(stream);
          }
        }
      }
      catch (WebException ex)
      {
        if (ex.Status == WebExceptionStatus.ProtocolError &&
            ex.Response != null)
        {
          var resp = (HttpWebResponse)ex.Response;
          if (resp.StatusCode == HttpStatusCode.NotFound)
          {
            Logger.Warn("404 - couldn't download image");
          }
          else
          {
            Logger.Warn("Unknown error while downloading an Image! Message: {0}", ex.Message);
          }
        }
        else
        {
          Logger.Warn("Unknown error while downloading an Image! Message: {0}", ex.Message);
        }
      }
      return buffer;
    }

    public static byte[] ReadFullStream(Stream input)
    {
      byte[] buffer = new byte[16 * 1024];
      using (MemoryStream ms = new MemoryStream())
      {
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
          ms.Write(buffer, 0, read);
        }
        return ms.ToArray();
      }
    }

    #region helpers

    public static DmapData ExtractMetaDataFromDidlLite(string metaData, out string coverUrl)
    {
      if (string.IsNullOrEmpty(metaData))
      {
        coverUrl = string.Empty;
        return null;
      }

      DmapData dmapData = new DmapData();
      List<string> directors = new List<string>();
      List<string> artists = new List<string>();
      List<string> actors = new List<string>();
      List<string> genres = new List<string>();
      coverUrl = string.Empty;


      // there is a bug in the xmlreader when there is no newline in the xml document
      // https://social.msdn.microsoft.com/Forums/silverlight/en-US/04cd225d-6c09-427b-9586-f81848b22e08/xml-parsing-problem-xmlrearderreadtofollowing-causes-problem?forum=silverlightnet
      /*
          Hi Steve,

           Thank you very much. I could solve my problem.

          The cause was that my xml did not contain any line breaks.

          When I rewrote the xml adding line breaks, the Xml.Reader.ReadToFollowing method could successfully find the all elements.

          Again, thank you very much for your help!
      */

      using (XmlReader reader = XmlReader.Create(new StringReader(metaData)))
      {
        while (reader.Read())
        {
          Logger.Debug("Name: {0} - Value: {1}", reader.Name, reader.Value);

          if (reader.NodeType == XmlNodeType.Element)
          {
            switch (reader.Name)
            {

              // DIDL-Lite
              // DC: http://www.upnp.org/schemas/av/didl-lite-v2.xsd

              case "dc:title":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  dmapData.Title = reader.Value;
                  Logger.Debug("Title value: {0}", dmapData.Title);
                }
                break;
              case "dc:description":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  dmapData.Description = reader.Value;
                  Logger.Debug("Description value: {0}", dmapData.Description);
                }
                break;
              case "dc:creator":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  dmapData.Creator = reader.Value;
                  Logger.Debug("Creator value: {0}", dmapData.Creator);
                }
                break;

              // UPnP: http://www.upnp.org/schemas/av/upnp.xsd
              // Contributor Related Properties

              case "upnp:director":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  directors.Add(reader.Value);
                  Logger.Debug("Director value: {0}", directors.Last());
                }
                break;
              case "upnp:artist":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  artists.Add(reader.Value);
                  Logger.Debug("Artists value: {0}", artists.Last());
                }
                break;
              case "upnp:actor":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  actors.Add(reader.Value);
                  Logger.Debug("Actors value: {0}", actors.Last());
                }
                break;

              // missing: author, producer

              //Affiliation Related Properties

              case "upnp:album":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  dmapData.Album = reader.Value;
                  Logger.Debug("Album value: {0}", dmapData.Album);
                }
                break;
              case "upnp:genre":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  genres.Add(reader.Value);
                  Logger.Debug("Genres value: {0}", genres.Last());
                }
                break;

              // missing: playlist

              // Associated Resources Properties 

              case "upnp:albumArtURI":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  coverUrl = reader.Value;
                  Logger.Debug("CoverURL value: {0}", coverUrl);
                }
                break;

              // missing: artistDiscographyURI, lyricsURI

              // missing: Storage Related Properties, General Description Properties, Recorded Object Related Properties, User Channel and EPG Related Properties 
              //          Radio Broadcast Properties, Video Broadcast Properties, Physical Tuner Status-related Properties, Bookmark Related Properties
              //          Foreign Metadata Related Properties, Miscellaneous Properties, Object Tracking Properties, Content Protection Properties
              //          Base Properties, Contributor Related Properties, Affiliation Related Properties, User Channel and EPG Related Properties
              //          Video Broadcast Properties, ... to the end

              case "upnp:originalTrackNumber":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  int number;
                  dmapData.OriginalTrackNumber = int.TryParse(reader.Value, out number) ? number : 0;
                  Logger.Debug("OriginalTrackNumber value: {0}", dmapData.OriginalTrackNumber);
                }
                break;
              case "upnp:originalDiscNumber":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  int number;
                  dmapData.OriginalDiscNumber = int.TryParse(reader.Value, out number) ? number : 0;
                  Logger.Debug("OriginalDiscNumber value: {0}", dmapData.OriginalDiscNumber);
                }
                break;
              case "upnp:originalDiscCount":
                reader.Read();
                if (reader.Value != string.Empty)
                {
                  int number;
                  dmapData.OriginalDiscCount = int.TryParse(reader.Value, out number) ? number : 0;
                  Logger.Debug("OriginalDiscCount value: {0}", dmapData.OriginalDiscCount);
                }
                break;

            }
          }
        }


        /*reader.ReadToFollowing("dc:title");
        dmapData.Title = reader.ReadElementContentAsString();

        reader.ReadToFollowing("upnp:artist");
        dmapData.Artists = new[] { reader.ReadElementContentAsString() };

        reader.ReadToFollowing("upnp:albumArtURI");
        coverUrl = reader.ReadElementContentAsString();*/
      }

      dmapData.Directors = directors.ToArray();
      dmapData.Artists = artists.ToArray();
      dmapData.Actors = actors.ToArray();
      dmapData.Genres = genres.ToArray();

      return dmapData;
    }

    private static string GetMimeFromRegistry(string filename)
    {
      string mime = "application/octetstream";
      var extension = Path.GetExtension(filename);
      if (extension != null)
      {
        string ext = extension.ToLower();
        RegistryKey rk = Registry.ClassesRoot.OpenSubKey(ext);
        if (rk != null && rk.GetValue("Content Type") != null)
          mime = rk.GetValue("Content Type").ToString();
      }
      return mime;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    #endregion helpers
  }
}
