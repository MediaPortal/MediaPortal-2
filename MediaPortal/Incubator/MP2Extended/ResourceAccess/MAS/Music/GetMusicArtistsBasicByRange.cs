using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  // TODO: Rework after MIA Rework
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetMusicArtistsBasicByRange
  {
    public IList<WebMusicArtistBasic> Process(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      // we can't select only for shows, so we take all episodes and filter.
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(AudioAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Audioitems found");

      List<WebMusicArtistBasic> output = new List<WebMusicArtistBasic>();

      foreach (var item in items)
      {
        MediaItemAspect audioAspects = item[AudioAspect.Metadata];

        var albumArtists = (HashSet<object>)audioAspects[AudioAspect.ATTR_ALBUMARTISTS];
        if (albumArtists != null)
          foreach (var artist in albumArtists.Cast<string>())
          {
            if (output.FindIndex(x => x.Title == artist) == -1)
              output.Add(new WebMusicArtistBasic
              {
                Id = Convert.ToBase64String((new UTF8Encoding().GetBytes(artist))),
                Title = artist,
                PID = 0,
                HasAlbums = true  // TODO: rework
              });
          }
      }

      // sort and filter
      if (sort != null && order != null)
      {
       output = output.AsQueryable().Filter(filter).SortMediaItemList(sort, order).ToList();
      }
      else
        output = output.AsQueryable().Filter(filter).ToList();

      Logger.Info("GetTVShowsBasicByRange: start: {0}, end: {1}", start, end);

      if (start == null || end == null)
        throw new BadRequestException("start or end parameter is missing");

      output = output.TakeRange(start, end).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}