using System;
using System.Collections.Generic;
using System.Linq;
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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  // TODO: Rework after MIA Rework
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetMusicArtistCount
  {
    public WebIntResult Process(string filter)
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

      IList<string> artists = new List<string>();

      foreach (var item in items)
      {
        MediaItemAspect audioAspects = item[AudioAspect.Metadata];

        var albumArtists = (HashSet<object>)audioAspects[AudioAspect.ATTR_ALBUMARTISTS];
        if (albumArtists != null)
          foreach (var artist in albumArtists.Cast<string>())
          {
            if (!artists.Contains(artist))
              artists.Add(artist);
          }
      }

      // sort and filter
      if (filter != null)
        artists = artists.Filter(filter).ToList();

      return new WebIntResult { Result = artists.Count};
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}