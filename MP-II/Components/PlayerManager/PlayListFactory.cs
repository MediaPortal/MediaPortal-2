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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Presentation.Players;

using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManager;

using Components.Services.PlayerManager.PlayListIO;

namespace Components.Services.PlayerManager
{
  public class PlaylistFactory : IPlaylistFactory
  {
    #region IPlayListFactory Members

    /// <summary>
    /// Creates a new playlist.
    /// </summary>
    /// <returns>new playlist</returns>
    public IPlaylist LoadPlayList(string fileName)
    {
      PlayList playlist = new PlayList();
      IPlaylistIO playlistio = null;
      string extension = System.IO.Path.GetExtension(fileName).ToLower();

      switch (extension)
      {
        case ".m3u" :
          playlistio = new PlayListM3uIO();
          break;

        case ".pls" :
          playlistio = new PlayListPLSIO();
          break;

        case ".b4s" :
          break;

        case ".wpl" :
          break;
      }

      if (playlistio != null)
      {
        IList<IAbstractMediaItem> fileItems;
        if ((fileItems = playlistio.Load(fileName)) != null)
        {
          // Now let's get the tags of all the files found
          IImporterManager mgr = ServiceScope.Get<IImporterManager>();
          mgr.GetMetaDataFor("", ref fileItems);
          foreach (IAbstractMediaItem item in fileItems)
          {
            playlist.Add((IMediaItem)item);
          }
        }
      }
      return playlist;
    }

    #endregion
  }
}
