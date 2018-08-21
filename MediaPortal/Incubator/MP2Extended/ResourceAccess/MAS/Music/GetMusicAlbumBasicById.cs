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
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  // TODO: Hack, rework after MIA rework
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetMusicAlbumBasicById
  {
    public Task<WebMusicAlbumBasic> ProcessAsync(IOwinContext context, Guid id)
    {
      if (id == null)
        throw new BadRequestException("GetMusicTrackBasicById: id is null");

      // Get all tracks for this Album
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(AudioAspect.ASPECT_ID);

      IList<MediaItem> tracks = MediaLibraryAccess.GetMediaItemsByGroup(context, AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, id, necessaryMIATypes, null);
      if (tracks.Count == 0)
        throw new BadRequestException("No Tracks found");

      MediaItemAspect audioAspects = MediaItemAspect.GetAspect(tracks[0].Aspects, AudioAspect.Metadata);

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
      //var trackGenres = (HashSet<object>)audioAspects[AudioAspect.ATTR_GENRES];
      //if (trackGenres != null)
      //  webMusicAlbumBasic.Genres = trackGenres.Cast<string>().ToList();
      //webMusicTrackBasic.Rating = Convert.ToSingle((double)movieAspects[AudioAspect.]);
      //webMusicTrackBasic.Year;
      //webMusicTrackBasic.Artwork;
      DateTime dateAdded;
      if (MediaItemAspect.TryGetAttribute(tracks[0].Aspects, ImporterAspect.ATTR_DATEADDED, out dateAdded))
        webMusicAlbumBasic.DateAdded = dateAdded;
      webMusicAlbumBasic.Id = id.ToString();
      webMusicAlbumBasic.PID = 0;
      //webMusicTrackBasic.Path;
      webMusicAlbumBasic.Title = id.ToString();

      return Task.FromResult(webMusicAlbumBasic);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
