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

using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses;
using MediaPortal.Plugins.MP2Extended.MAS.Music;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "playlistId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetPlaylistItems : BaseMusicTrackBasic
  {
    public static Task<IList<WebPlaylistItem>> ProcessAsync(IOwinContext context, string playlistId, string filter, WebSortField? sort, WebSortOrder? order)
    {
      // get the playlist
      PlaylistRawData playlistRawData = ServiceRegistration.Get<IMediaLibrary>().ExportPlaylist(Guid.Parse(playlistId));

      IFilter searchFilter = new MediaItemIdFilter(playlistRawData.MediaItemIds);
      IList<MediaItem> items = MediaLibraryAccess.Search(context, BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, searchFilter);
      if (items.Count == 0)
        return System.Threading.Tasks.Task.FromResult<IList<WebPlaylistItem>>(new List<WebPlaylistItem>());

      var tracks = items.Select(item => MusicTrackBasic(item))
        .Filter(filter);

      // sort and filter
      if (sort != null && order != null)
        tracks = tracks.SortWebMusicTrackBasic(sort, order);

      var output = tracks.Select(t => new WebPlaylistItem(t));

      return System.Threading.Tasks.Task.FromResult<IList<WebPlaylistItem>>(output.ToList());
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
