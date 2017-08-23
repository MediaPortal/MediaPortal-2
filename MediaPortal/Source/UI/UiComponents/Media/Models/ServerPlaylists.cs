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
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.Models
{
  public class ServerPlaylists
  {
    protected static IContentDirectory GetContentDirectoryService()
    {
      IContentDirectory contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory != null)
        return contentDirectory;
      throw new NotConnectedException();
    }

    public static ICollection<PlaylistInformationData> GetPlaylists()
    {
      IContentDirectory contentDirectory = GetContentDirectoryService();
      return contentDirectory == null ? new List<PlaylistInformationData>(0) :
          contentDirectory.GetPlaylists();
    }

    public static void SavePlaylist(PlaylistRawData playlistData)
    {
      IContentDirectory contentDirectory = GetContentDirectoryService();
      contentDirectory.SavePlaylist(playlistData);
    }

    public static void RemovePlaylists(ICollection<Guid> playlistIds)
    {
      IContentDirectory contentDirectory = GetContentDirectoryService();
      foreach (Guid playlistId in playlistIds)
        contentDirectory.DeletePlaylist(playlistId);
    }
  }
}
