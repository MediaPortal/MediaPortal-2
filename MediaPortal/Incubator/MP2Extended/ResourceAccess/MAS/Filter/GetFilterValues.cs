using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Filters;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.Utils;

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
  internal class GetFilterValues : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string mediaType = httpParam["mediaType"].Value;
      string filterField = httpParam["filterField"].Value;
      //string provider = httpParam["provider"].Value;
      string op = httpParam["op"].Value;
      string limit = httpParam["limit"].Value;
      string order = httpParam["order"].Value;

      WebMediaType mediaTypeEnum;
      if (mediaType == null || !Enum.TryParse(mediaType, out mediaTypeEnum))
        throw new BadRequestException("GetFilterValues: mediaType is null");

      if (filterField == null)
        throw new BadRequestException("GetFilterValues: filterField is null");

      int? limitInt = NullableHelper.ParseNullable<int>(limit, int.TryParse);
      WebSortOrder? webSortOrder = NullableHelper.ParseNullable<WebSortOrder>(order, Enum.TryParse);

      switch (mediaTypeEnum)
      {
        case WebMediaType.Drive:
          return AutoSuggestion.GetValuesForField(filterField, (List<WebDriveBasic>)(new GetFileSystemDrives().Process(request, session)), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.Movie:
        //return AutoSuggestion.GetValuesForField(filterField, new GetMoviesDetailed().Process(provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.MusicAlbum:
        //return AutoSuggestion.GetValuesForField(filterField, new GetMusicAlbumsBasic().Process(provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.MusicArtist:
        //return AutoSuggestion.GetValuesForField(filterField, GetMusicArtistsDetailed(provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.MusicTrack:
        //return AutoSuggestion.GetValuesForField(filterField, GetMusicTracksDetailed(provider), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.Picture:
          return AutoSuggestion.GetValuesForField(filterField, (List<WebPictureDetailed>)(new GetPicturesDetailed().Process(request, session)), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
        case WebMediaType.Playlist:
          return AutoSuggestion.GetValuesForField(filterField, (List<WebPlaylist>)(new GetPlaylists().Process(request, session)), op, limitInt).OrderBy(x => x, webSortOrder).ToList();
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