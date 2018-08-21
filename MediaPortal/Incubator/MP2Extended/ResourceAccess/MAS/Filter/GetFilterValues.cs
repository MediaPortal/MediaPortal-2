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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Filters;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.Extensions;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Filter
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<string>),
    Summary = "Get all available values for a given field")]
  [ApiFunctionParam(Name = "mediaType", Type = typeof(WebMediaType), Nullable = false)]
  [ApiFunctionParam(Name = "filterField", Type = typeof(string), Nullable = false)]
  //[ApiFunctionParam(Name = "provider", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "op", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "limit", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  // TODO: add the missing functions once these are implemented
  internal class GetFilterValues
  {
    public async Task<IList<string>> ProcessAsync(IOwinContext context, WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
      switch (mediaType)
      {
        case WebMediaType.Drive:
          return AutoSuggestion.GetValuesForField(filterField, await new GetFileSystemDrives().ProcessAsync(context, null, null), op, limit).OrderBy(x => x, order).ToList();
        case WebMediaType.Movie:
        //return AutoSuggestion.GetValuesForField(filterField, new GetMoviesDetailed().ProcessAsync(context, provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.MusicAlbum:
        //return AutoSuggestion.GetValuesForField(filterField, new GetMusicAlbumsBasic().ProcessAsync(context, provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.MusicArtist:
        //return AutoSuggestion.GetValuesForField(filterField, GetMusicArtistsDetailed(provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.MusicTrack:
        //return AutoSuggestion.GetValuesForField(filterField, GetMusicTracksDetailed(provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.Picture:
          return AutoSuggestion.GetValuesForField(filterField, await new GetPicturesDetailed().ProcessAsync(context, null, null, null), op, limit).OrderBy(x => x, order).ToList();
        case WebMediaType.Playlist:
          return AutoSuggestion.GetValuesForField(filterField, await new GetPlaylists().ProcessAsync(context), op, limit).OrderBy(x => x, order).ToList();
        case WebMediaType.TVEpisode:
        //return AutoSuggestion.GetValuesForField(filterField, GetTVEpisodesDetailed(provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.TVShow:
        //return AutoSuggestion.GetValuesForField(filterField, GetTVShowsDetailed(provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        default:
          throw new BadRequestException(string.Format("GetFilterValues() called with unsupported mediaType='{0}' filterField='{1}' op='{2}' limit='{3}'", mediaType, filterField, op, limit));
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
