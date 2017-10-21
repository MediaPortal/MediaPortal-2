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
using MediaPortal.Plugins.MP2Extended.MAS.Music;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetMusicTrackBasicById
  {
    public WebMusicTrackBasic Process(Guid id)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(AudioAspect.ASPECT_ID);

      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetMusicTrackBasicById: No MediaItem found with id: {0}", id));

      MediaItemAspect audioAspects = item[AudioAspect.Metadata];

      WebMusicTrackBasic webMusicTrackBasic = new WebMusicTrackBasic();
      webMusicTrackBasic.Album = (string)audioAspects[AudioAspect.ATTR_ALBUM];
      var albumArtists = (HashSet<object>)audioAspects[AudioAspect.ATTR_ALBUMARTISTS];
      if (albumArtists != null)
        webMusicTrackBasic.AlbumArtist = String.Join(", ", albumArtists.Cast<string>().ToArray());
      //webMusicTrackBasic.AlbumArtistId;
      // TODO: We have to wait for the MIA Rework, until than the ID is just the name as bas64
      webMusicTrackBasic.AlbumId = Convert.ToBase64String((new UTF8Encoding()).GetBytes((string)audioAspects[AudioAspect.ATTR_ALBUM]));
      var trackArtists = (HashSet<object>)audioAspects[AudioAspect.ATTR_ARTISTS];
      if (albumArtists != null)
        webMusicTrackBasic.Artist = trackArtists.Cast<string>().ToList();
      //webMusicTrackBasic.ArtistId;
      webMusicTrackBasic.DiscNumber = audioAspects[AudioAspect.ATTR_DISCID] != null ? (int)audioAspects[AudioAspect.ATTR_DISCID] : 0;
      webMusicTrackBasic.Duration = Convert.ToInt32((long)audioAspects[AudioAspect.ATTR_DURATION]);
      var trackGenres = (HashSet<object>)audioAspects[AudioAspect.ATTR_GENRES];
      if (trackGenres != null)
        webMusicTrackBasic.Genres = trackGenres.Cast<string>().ToList();
      //webMusicTrackBasic.Rating = Convert.ToSingle((double)movieAspects[AudioAspect.]);
      webMusicTrackBasic.TrackNumber = (int)audioAspects[AudioAspect.ATTR_TRACK];
      webMusicTrackBasic.Type = WebMediaType.MusicTrack;
      //webMusicTrackBasic.Year;
      //webMusicTrackBasic.Artwork;
      webMusicTrackBasic.DateAdded = (DateTime)item[ImporterAspect.Metadata][ImporterAspect.ATTR_DATEADDED];
      webMusicTrackBasic.Id = item.MediaItemId.ToString();
      webMusicTrackBasic.PID = 0;
      //webMusicTrackBasic.Path;
      webMusicTrackBasic.Title = (string)item[MediaAspect.Metadata][MediaAspect.ATTR_TITLE];


      return webMusicTrackBasic;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}