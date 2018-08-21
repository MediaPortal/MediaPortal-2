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
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.Extensions;
using Microsoft.Owin;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetMusicTracksBasicForAlbum
  {
    public Task<IList<WebMusicTrackBasic>> ProcessAsync(IOwinContext context, Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      if (id == null)
        throw new BadRequestException("GetMusicTracksBasicForAlbum: no id is null");

      // Get all episodes for this
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(AudioAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      IList<MediaItem> tracks = MediaLibraryAccess.GetMediaItemsByGroup(context, AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, id, necessaryMIATypes, null);
      if (tracks.Count == 0)
        throw new BadRequestException("No Tracks found");

      var output = new List<WebMusicTrackBasic>();

      foreach (var item in tracks)
      {
        MediaItemAspect audioAspects = item.GetAspect(AudioAspect.Metadata);

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
        //var trackGenres = (HashSet<object>)audioAspects[AudioAspect.ATTR_GENRES];
        //if (trackGenres != null)
        //  webMusicTrackBasic.Genres = trackGenres.Cast<string>().ToList();
        //webMusicTrackBasic.Rating = Convert.ToSingle((double)movieAspects[AudioAspect.]);
        webMusicTrackBasic.TrackNumber = (int)audioAspects[AudioAspect.ATTR_TRACK];
        webMusicTrackBasic.Type = WebMediaType.MusicTrack;
        //webMusicTrackBasic.Year;
        //webMusicTrackBasic.Artwork;
        webMusicTrackBasic.DateAdded = item.GetAspect(ImporterAspect.Metadata).GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED);
        webMusicTrackBasic.Id = item.MediaItemId.ToString();
        webMusicTrackBasic.PID = 0;
        //webMusicTrackBasic.Path;
        webMusicTrackBasic.Title = item.GetAspect(MediaAspect.Metadata).GetAttributeValue<string>(MediaAspect.ATTR_TITLE);

        output.Add(webMusicTrackBasic);
      }

      // sort
      if (sort != null && order != null)
      {
        output = output.SortWebMusicTrackBasic(sort, order).ToList();
      }

      return Task.FromResult<IList<WebMusicTrackBasic>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
