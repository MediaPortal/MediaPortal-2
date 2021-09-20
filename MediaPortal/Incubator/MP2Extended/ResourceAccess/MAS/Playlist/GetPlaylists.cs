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

using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebPlaylist>), Summary = "")]
  internal class GetPlaylists
  {
    public static Task<IList<WebPlaylist>> ProcessAsync(IOwinContext context)
    {
      ICollection<PlaylistInformationData> playlists = ServiceRegistration.Get<IMediaLibrary>().GetPlaylists();

      List<WebPlaylist> output = new List<WebPlaylist>();

      foreach (var playlist in playlists)
      {
        WebPlaylist webPlaylist = new WebPlaylist
        {
          ItemCount = playlist.NumItems,
          Type = WebMediaType.Playlist,
          Id = playlist.PlaylistId.ToString(),
          Title = playlist.Name
        };
        //webPlaylist.Artwork;
        //webPlaylist.DateAdded;
        //webPlaylist.Path;

        output.Add(webPlaylist);
      }

      return Task.FromResult<IList<WebPlaylist>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
