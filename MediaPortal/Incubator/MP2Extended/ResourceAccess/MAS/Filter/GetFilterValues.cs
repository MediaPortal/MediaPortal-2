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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Filters;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow;
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
    public static async Task<IList<string>> ProcessAsync(IOwinContext context, WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
      switch (mediaType)
      {
        case WebMediaType.Drive:
          return AutoSuggestion.GetValuesForField(filterField, await GetFileSystemDrives.ProcessAsync(context, null, order), op, limit).ToList();
        case WebMediaType.Movie:
          return AutoSuggestion.GetValuesForField(filterField, await GetMoviesDetailed.ProcessAsync(context, null, null, order), op, limit).ToList();
        case WebMediaType.MusicAlbum:
          return AutoSuggestion.GetValuesForField(filterField, await GetMusicAlbumsBasic.ProcessAsync(context, null, null, order), op, limit).ToList();
        case WebMediaType.MusicArtist:
          return AutoSuggestion.GetValuesForField(filterField, await GetMusicArtistsDetailed.ProcessAsync(context, null, null, order), op, limit).ToList();
        case WebMediaType.MusicTrack:
          return AutoSuggestion.GetValuesForField(filterField, await GetMusicTracksDetailed.ProcessAsync(context, null, null, order), op, limit).ToList();
        case WebMediaType.Picture:
          return AutoSuggestion.GetValuesForField(filterField, await GetPicturesDetailed.ProcessAsync(context, null, null, null), op, limit).ToList();
        case WebMediaType.Playlist:
          return AutoSuggestion.GetValuesForField(filterField, await GetPlaylists.ProcessAsync(context), op, limit).ToList();
        case WebMediaType.TVEpisode:
          return AutoSuggestion.GetValuesForField(filterField, await GetTVEpisodesDetailed.ProcessAsync(context, null, null, order), op, limit).ToList();
        case WebMediaType.TVShow:
          return AutoSuggestion.GetValuesForField(filterField, await GetTVShowsDetailed.ProcessAsync(context, null, null, order), op, limit).ToList();
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
