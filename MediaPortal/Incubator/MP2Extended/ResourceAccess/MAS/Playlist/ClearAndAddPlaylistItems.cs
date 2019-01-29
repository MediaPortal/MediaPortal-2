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
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  // this function ignores the porsiton argument, the documentation is not very clear
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "playlistId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "position", Type = typeof(int), Nullable = true)]
  internal class ClearAndAddPlaylistItems
  {
    public static Task<WebBoolResult> ProcessAsync(IOwinContext context, string playlistId, WebMediaType type, int? position, List<string> ids)
    {
      if (ids.Count == 0)
        throw new BadRequestException(String.Format("AddPlaylistItems: id array is empty - itemIds: {0}", ids));

     // get the playlist
      PlaylistRawData playlistRawData = ServiceRegistration.Get<IMediaLibrary>().ExportPlaylist(Guid.Parse(playlistId));

      // clear the data
      playlistRawData.MediaItemIds.Clear();

      // insert the data
      foreach (var itemId in ids)
      {
       playlistRawData.MediaItemIds.Add(Guid.Parse(itemId));
      }

      // save playlist
      ServiceRegistration.Get<IMediaLibrary>().SavePlaylist(playlistRawData);

      return Task.FromResult(new WebBoolResult { Result = true });
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
