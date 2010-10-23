#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.PathManager;

namespace MediaPortal.UiComponents.Media.Models
{
  public class LocalPlaylists
  {
    #region Constants

    public const string PLAYLIST_FILE_EXTENSION = ".mpp";

    public static readonly XmlReaderSettings DEFAULT_XML_READER_SETTINGS = new XmlReaderSettings
      {
          CheckCharacters = false,
          IgnoreComments = true
      };

    public static readonly XmlWriterSettings DEFAULT_XML_WRITER_SETTINGS = new XmlWriterSettings
      {
          CloseOutput = true,
          Encoding = Encoding.UTF8,
          Indent = true,
          IndentChars = " ",
          NewLineChars = "\n",
          NewLineHandling = NewLineHandling.None
      };

    #endregion

    #region Protected fields

    /// <summary>
    /// Contains a dictionary of playlist file paths mapped to the playlist data instances.
    /// </summary>
    protected IDictionary<string, PlaylistRawData> _playlists = new Dictionary<string, PlaylistRawData>();

    #endregion

    public ICollection<PlaylistRawData> Playlists
    {
      get { return _playlists.Values; }
    }

    public void Refresh()
    {
      string[] playlistPaths = Directory.GetFiles(GetLocalPlaylistsPath());
      foreach (string playlistPath in playlistPaths)
        using (XmlReader reader = XmlReader.Create(playlistPath, DEFAULT_XML_READER_SETTINGS))
          try
          {
            reader.MoveToContent();
            PlaylistRawData playlistData = PlaylistRawData.Deserialize(reader);
            _playlists.Add(playlistPath, playlistData);
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("LocalPlaylists: Problem loading playlist '{0}'", e, playlistPath);
          }
    }

    public void SavePlaylist(PlaylistRawData playlistData, out string filePath)
    {
      string path = Path.Combine(GetLocalPlaylistsPath(), filePath = CreateFilename(playlistData.Name));
      using (XmlWriter writer = XmlWriter.Create(path, DEFAULT_XML_WRITER_SETTINGS))
        playlistData.Serialize(writer);
      _playlists.Add(filePath, playlistData);
    }

    public void RemovePlaylists(ICollection<Guid> playlistIds)
    {
      ICollection<string> deleteFilePaths = new List<string>(playlistIds.Count);
      foreach (KeyValuePair<string, PlaylistRawData> kvp in _playlists)
        if (playlistIds.Contains(kvp.Value.PlaylistId))
          deleteFilePaths.Add(kvp.Key);
      foreach (string filePath in deleteFilePaths)
      {
        File.Delete(filePath);
        _playlists.Remove(filePath);
      }
    }

    protected string CreateFilename(string playlistName)
    {
      ICollection<char> invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
      StringBuilder result = new StringBuilder(playlistName);
      for (int i = 0; i < result.Length; i++)
      {
        if (invalidChars.Contains(result[i]))
          result[i] = '_';
      }
      string fileName = result.ToString();
      if (!fileName.EndsWith(PLAYLIST_FILE_EXTENSION))
        fileName = fileName + PLAYLIST_FILE_EXTENSION;
      return fileName;
    }

    protected static string GetLocalPlaylistsPath()
    {
      IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
      string result = pathManager.GetPath("<PLAYLISTS>");
      Directory.CreateDirectory(result);
      return result;
    }
  }
}
