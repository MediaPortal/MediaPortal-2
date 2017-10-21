#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System.IO;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg
{
  public class FFMpegPlaylistManifest : PlaylistManifest
  {
    public const string PLAYLIST_FOLDER_SUFFIX = "_mptf";

    internal static string GetPlaylistFolderFromTranscodeFile(string cachePath, string transcodingFile)
    {
      string folderTranscodeId = Path.GetFileNameWithoutExtension(transcodingFile).Replace(".", "_") + PLAYLIST_FOLDER_SUFFIX;
      return Path.Combine(cachePath, folderTranscodeId);
    }

    internal static void CreatePlaylistFiles(FFMpegTranscodeData data)
    {
      if (Directory.Exists(data.WorkPath) == false)
      {
        Directory.CreateDirectory(data.WorkPath);
      }
      if (data.SegmentPlaylistData != null)
      {
        string playlist = Path.Combine(data.WorkPath, MediaConverter.PLAYLIST_FILE_NAME);
        string tempPlaylist = playlist + ".tmp";
        File.WriteAllBytes(tempPlaylist, data.SegmentPlaylistData);
        File.Move(tempPlaylist, playlist);
        if (data.SegmentSubsPlaylistData != null)
        {
          playlist = Path.Combine(data.WorkPath, MediaConverter.PLAYLIST_SUBTITLE_FILE_NAME);
          tempPlaylist = playlist + ".tmp";
          File.WriteAllBytes(playlist, data.SegmentSubsPlaylistData);
          File.Move(tempPlaylist, playlist);
        }
      }
      if (data.SegmentPlaylist != null && data.SegmentManifestData != null)
      {
        string tempManifest = data.SegmentPlaylist + ".tmp";
        File.WriteAllBytes(tempManifest, data.SegmentManifestData);
        File.Move(tempManifest, data.SegmentPlaylist);
      }

      //No need to keep data so free used memory
      data.SegmentManifestData = null;
      data.SegmentPlaylistData = null;
      data.SegmentSubsPlaylistData = null;
    }
  }
}
