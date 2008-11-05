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
  /// Loads a Playlist in PLS Format
  /// </summary>
  class PlayListPLSIO : IPlaylistIO
  {
    const string START_PLAYLIST_MARKER = "[playlist]";
    const string PLAYLIST_NAME = "PlaylistName";

    private StreamReader file;
    private List<IAbstractMediaItem> fileset;

    public PlayListPLSIO()
    {
      fileset = new List<IAbstractMediaItem>();
    }

    /// <summary>
    /// Parses and Loads the PLS Playlist
    /// </summary>
    /// <param name="playlistFileName"></param>
    /// <returns>List of items in Playlist as IMediaitem</returns>
    public List<IAbstractMediaItem> Load(string playlistFileName)
    {
      string basePath;

      if (playlistFileName == null)
        return null;

      try
      {
        basePath = Path.GetDirectoryName(Path.GetFullPath(playlistFileName));

        using (file = new StreamReader(playlistFileName, Encoding.Default))
        {
          if (file == null)
            return null;

          string line;
          line = file.ReadLine();
          if (line == null || line.Length == 0)
            return null;

          string strLine = line.Trim();

          if (strLine != START_PLAYLIST_MARKER)
          {
            PlayListItem newItem = new PlayListItem(strLine, strLine, 0);
            fileset.Add(newItem);
          }

          string infoLine = "";
          string durationLine = "";
          string fileName = "";
          line = file.ReadLine();
          while (line != null)
          {
            strLine = line.Trim();
            //CUtil::RemoveCRLF(strLine);
            int equalPos = strLine.IndexOf("=");
            if (equalPos > 0)
            {
              string leftPart = strLine.Substring(0, equalPos);
              equalPos++;
              string valuePart = strLine.Substring(equalPos);
              leftPart = leftPart.ToLower();
              if (leftPart.StartsWith("file"))
              {
                if (valuePart.Length > 0 && valuePart[0] == '#')
                {
                  line = file.ReadLine();
                  continue;
                }

                if (fileName.Length != 0)
                {
                  PlayListItem newItem = new PlayListItem(infoLine, fileName, 0);
                  fileset.Add(newItem);
                  fileName = "";
                  infoLine = "";
                  durationLine = "";
                }
                fileName = valuePart;
              }
              if (leftPart.StartsWith("title"))
              {
                infoLine = valuePart;
              }
              else
              {
                if (infoLine == "") infoLine = System.IO.Path.GetFileName(fileName);
              }
              if (leftPart.StartsWith("length"))
              {
                durationLine = valuePart;
              }

              if (durationLine.Length > 0 && infoLine.Length > 0 && fileName.Length > 0)
              {
                int duration = System.Int32.Parse(durationLine);

                string tmp = fileName.ToLower();
                FileUtils.CombinePaths(basePath, fileName);
                PlayListItem newItem = new PlayListItem(infoLine, fileName, duration);
                fileset.Add(newItem);
                fileName = "";
                infoLine = "";
                durationLine = "";
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

    public void Save(IPlaylist playlist, string fileName)
    {
      using (StreamWriter writer = new StreamWriter(fileName, false))
      {
        writer.WriteLine(START_PLAYLIST_MARKER);
        for (int i = 0; i < playlist.Queue.Count; i++)
        {
          PlayListItem item = (PlayListItem)playlist.Queue[i];
          writer.WriteLine("File{0}={1}", i + 1, item.FullPath);
          writer.WriteLine("Title{0}={1}", i + 1, item.Title);
          writer.WriteLine("Length{0}={1}", i + 1, item.MetaData["duration"]);
        }
        writer.WriteLine("NumberOfEntries={0}", playlist.Queue.Count);
        writer.WriteLine("Version=2");
      }
    }
  }
}
