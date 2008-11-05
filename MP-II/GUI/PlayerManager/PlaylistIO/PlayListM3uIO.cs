#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Media.MediaManagement;
using MediaPortal.Utilities.FileSystem;

namespace Components.Services.PlayerManager.PlayListIO
{
  /// <summary>
  /// Loads Playlist in M3U Format
  /// </summary>
  public class PlayListM3uIO : IPlaylistIO
  {
    const string M3U_START_MARKER = "#EXTM3U";
    const string M3U_INFO_MARKER = "#EXTINF";
    private List<IAbstractMediaItem> fileset;
    private StreamReader file;
    private string basePath;

    public PlayListM3uIO()
    {
      fileset = new List<IAbstractMediaItem>();
    }

    /// <summary>
    /// Parses and Loads the M3U Playlist
    /// </summary>
    /// <param name="playlistFileName"></param>
    /// <returns>List of items in Playlist as IMediaitem</returns>
    public List<IAbstractMediaItem> Load(string playlistFileName)
    {
      if (playlistFileName == null)
        return null;

      try
      {
        basePath = Path.GetDirectoryName(Path.GetFullPath(playlistFileName));

        using (file = new StreamReader(playlistFileName, Encoding.Default))
        {
          if (file == null)
            return null;

          string line = file.ReadLine();
          if (line == null || line.Length == 0)
            return null;

          string trimmedLine = line.Trim();

          if (trimmedLine != M3U_START_MARKER)
          {
            string fileName = trimmedLine;
            if (!AddItem("", 0, fileName))
              return null;
          }

          line = file.ReadLine();
          while (line != null)
          {
            trimmedLine = line.Trim();

            if (trimmedLine != "")
            {
              if (trimmedLine.StartsWith(M3U_INFO_MARKER))
              {
                string songName = null;
                int lDuration = 0;

                if (ExtractM3uInfo(trimmedLine, ref songName, ref lDuration))
                {
                  line = file.ReadLine();
                  if (!AddItem(songName, lDuration, line))
                    break;
                }
              }
              else
              {
                if (!AddItem("", 0, trimmedLine))
                  break;
              }
            }
            line = file.ReadLine();
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("Exception loading Playlist {0} err:{1} stack:{2}", playlistFileName, ex.Message, ex.StackTrace);
        fileset = null;
      }
      return fileset;
    }

    private static bool ExtractM3uInfo(string trimmedLine, ref string songName, ref int lDuration)
    {
      //bool successfull;
      int iColon = (int)trimmedLine.IndexOf(":");
      int iComma = (int)trimmedLine.IndexOf(",");
      if (iColon >= 0 && iComma >= 0 && iComma > iColon)
      {
        iColon++;
        string duration = trimmedLine.Substring(iColon, iComma - iColon);
        iComma++;
        songName = trimmedLine.Substring(iComma);
        lDuration = System.Int32.Parse(duration);
        return true;
      }
      return false;
    }


    private bool AddItem(string songName, int duration, string fileName)
    {
      if (fileName == null || fileName.Length == 0)
        return false;

      fileName = FileUtils.CombinePaths(basePath, fileName);
      PlayListItem newItem = new PlayListItem(songName, fileName, duration);
      fileset.Add(newItem);
      return true;
    }

    /// <summary>
    /// Saves the Playlist
    /// </summary>
    /// <param name="playlist"></param>
    /// <param name="fileName"></param>
    public void Save(IPlaylist playlist, string fileName)
    {
      try
      {
        using (StreamWriter writer = new StreamWriter(fileName, false))
        {
          writer.WriteLine(M3U_START_MARKER);

          foreach (PlayListItem item in playlist.Queue)
          {
            writer.WriteLine("{0}:{1},{2}", M3U_INFO_MARKER, item.MetaData["duration"], item.Title);
            writer.WriteLine("{0}", item.FullPath);
          }
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Failed to save a Playlist {0}. err: {1} stack: {2}", fileName, e.Message, e.StackTrace);
      }
    }
  }
}