using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  // TODO: Hack, rework after MIA rework
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetMusicAlbumBasicById
  {
    public WebMusicAlbumBasic Process(Guid id)
    {
      if (id == null)
        throw new BadRequestException("GetMusicTrackBasicById: id is null");

      // Get all tracks for this Album
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(AudioAspect.ASPECT_ID);

      IFilter searchFilter = new RelationalFilter(AudioAspect.ATTR_ALBUM, RelationalOperator.EQ, id);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, null, searchFilter);

      IList<MediaItem> tracks = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);

      if (tracks.Count == 0)
        throw new BadRequestException("No Tracks found");

      MediaItemAspect audioAspects = tracks[0][AudioAspect.Metadata];

      WebMusicAlbumBasic webMusicAlbumBasic = new WebMusicAlbumBasic();
      var albumArtists = (HashSet<object>)audioAspects[AudioAspect.ATTR_ALBUMARTISTS];
      if (albumArtists != null)
        webMusicAlbumBasic.AlbumArtist = String.Join(", ", albumArtists.Cast<string>().ToArray());
      //webMusicTrackBasic.AlbumArtistId;
      var trackArtists = (HashSet<object>)audioAspects[AudioAspect.ATTR_ARTISTS];
      if (albumArtists != null)
        webMusicAlbumBasic.Artists = trackArtists.Cast<string>().ToList();
      var trackComposers = (HashSet<object>)audioAspects[AudioAspect.ATTR_COMPOSERS];
      if (trackComposers != null)
        webMusicAlbumBasic.Composer = trackComposers.Cast<string>().ToList();
      //webMusicTrackBasic.ArtistId;
      var trackGenres = (HashSet<object>)audioAspects[AudioAspect.ATTR_GENRES];
      if (trackGenres != null)
        webMusicAlbumBasic.Genres = trackGenres.Cast<string>().ToList();
      //webMusicTrackBasic.Rating = Convert.ToSingle((double)movieAspects[AudioAspect.]);
      //webMusicTrackBasic.Year;
      //webMusicTrackBasic.Artwork;
      webMusicAlbumBasic.DateAdded = (DateTime)tracks[0][ImporterAspect.Metadata][ImporterAspect.ATTR_DATEADDED];
      webMusicAlbumBasic.Id = id.ToString();
      webMusicAlbumBasic.PID = 0;
      //webMusicTrackBasic.Path;
      webMusicAlbumBasic.Title = id.ToString();


      return webMusicAlbumBasic;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}