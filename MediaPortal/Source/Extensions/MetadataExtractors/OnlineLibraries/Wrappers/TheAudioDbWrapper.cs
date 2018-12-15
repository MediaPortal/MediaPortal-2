#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class TheAudioDbWrapper : ApiWrapper<string, string>
  {
    protected AudioDbApiV1 _audioDbHandler;

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath)
    {
      _audioDbHandler = new AudioDbApiV1("912057237373f620001833", cachePath);
      SetDefaultLanguage(AudioDbApiV1.DefaultLanguage);
      SetCachePath(cachePath);
      return true;
    }

    #region Search

    public override async Task<List<TrackInfo>> SearchTrackAsync(TrackInfo trackSearch, string language)
    {
      language = language ?? PreferredLanguage;

      List<AudioDbTrack> foundTracks = null;
      foreach (PersonInfo person in trackSearch.AlbumArtists)
      {
        foundTracks = await _audioDbHandler.SearchTrackAsync(person.Name, trackSearch.TrackName, language).ConfigureAwait(false);
        if (foundTracks != null)
          break;
      }
      if (foundTracks == null)
      {
        foreach (PersonInfo person in trackSearch.Artists)
        {
          foundTracks = await _audioDbHandler.SearchTrackAsync(person.Name, trackSearch.TrackName, language).ConfigureAwait(false);
          if (foundTracks != null)
            break;
        }
      }
      if (foundTracks == null && !string.IsNullOrEmpty(trackSearch.MusicBrainzId))
      {
        //Use last because the MusicBarainz id is used for finding a track from a random album
        var track = await _audioDbHandler.GetTrackByMbidAsync(trackSearch.MusicBrainzId, language, false).ConfigureAwait(false);
        if (track != null)
          foundTracks = new List<AudioDbTrack> { track };
      }
      if (foundTracks == null) return null;

      List<TrackInfo> tracks = null;
      foreach (AudioDbTrack track in foundTracks)
      {
        if (tracks == null)
          tracks = new List<TrackInfo>();

        TrackInfo info = new TrackInfo()
        {
          AudioDbId = track.TrackID,
          AlbumAudioDbId = track.AlbumID ?? 0,
          MusicBrainzId = track.MusicBrainzID,
          AlbumMusicBrainzGroupId = track.MusicBrainzAlbumID,
          
          TrackName = track.Track,
          TrackNum = track.TrackNumber,
          Album = track.Album,
          Artists = ConvertToPersons(track.ArtistID ?? 0, track.MusicBrainzArtistID, track.Artist, PersonAspect.OCCUPATION_ARTIST, track.Track, track.Album),
        };
        tracks.Add(info);
      }

      return tracks;
    }

    public override async Task<List<AlbumInfo>> SearchTrackAlbumAsync(AlbumInfo albumSearch, string language)
    {
      language = language ?? PreferredLanguage;

      List<AudioDbAlbum> foundAlbums = null;
      if(albumSearch.Artists.Count == 0)
        foundAlbums = await _audioDbHandler.SearchAlbumAsync("", albumSearch.Album, language).ConfigureAwait(false);
      foreach (PersonInfo person in albumSearch.Artists)
      {
        foundAlbums = await _audioDbHandler.SearchAlbumAsync(person.Name, albumSearch.Album, language).ConfigureAwait(false);
        if (foundAlbums != null)
          break;
      }
      if (foundAlbums == null) return null;

      List<AlbumInfo> albums = null;
      foreach (AudioDbAlbum album in foundAlbums)
      {
        if (albums == null)
          albums = new List<AlbumInfo>();

        AlbumInfo info = new AlbumInfo()
        {
          AudioDbId = album.AlbumId,
          MusicBrainzGroupId = album.MusicBrainzID,
          
          Album = album.Album,
          ReleaseDate = album.Year != null && album.Year.Value > 1900 ? new DateTime(album.Year.Value, 1, 1) : default(DateTime?),
          Artists = ConvertToPersons(album.ArtistId ?? 0, album.MusicBrainzArtistID, album.Artist, PersonAspect.OCCUPATION_ARTIST, null, album.Album),
          MusicLabels = ConvertToCompanies(album.LabelId ?? 0, album.Label, CompanyAspect.COMPANY_MUSIC_LABEL),
        };
        albums.Add(info);
      }

      return albums;
    }

    public override async Task<List<PersonInfo>> SearchPersonAsync(PersonInfo personSearch, string language)
    {
      language = language ?? PreferredLanguage;

      if (personSearch.Occupation != PersonAspect.OCCUPATION_ARTIST)
        return null;

      List<AudioDbArtist> foundArtists = await _audioDbHandler.SearchArtistAsync(personSearch.Name, language).ConfigureAwait(false);
      if (foundArtists == null) return null;

      List<PersonInfo> persons = null;
      foreach (AudioDbArtist artist in foundArtists)
      {
        if (persons == null)
          persons = new List<PersonInfo>();

        PersonInfo info = new PersonInfo()
        {
          AudioDbId = artist.ArtistId,
          MusicBrainzId = artist.MusicBrainzID,
          Name = artist.Artist,
          AlternateName = artist.ArtistAlternate,
          Occupation = PersonAspect.OCCUPATION_ARTIST,
          IsGroup = artist.Members.HasValue ? artist.Members.Value > 1 : false,
        };
        persons.Add(info);
      }

      return persons;
    }

    #endregion

    #region Convert

    private List<PersonInfo> ConvertToPersons(long artistId, string mbArtistId, string artist, string occupation, string track, string album)
    {
      if (artistId == 0 || string.IsNullOrEmpty(artist))
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      int sortOrder = 0;
      if (artistId > 0)
      {
        retValue.Add(
        new PersonInfo()
        {
          AudioDbId = artistId,
          MusicBrainzId = mbArtistId,
          Name = artist,
          Occupation = occupation,
          Order = sortOrder++,
          MediaName = track,
          ParentMediaName = album
        });

      }
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanies(long companyId, string company, string type)
    {
      if (companyId == 0 || string.IsNullOrEmpty(company))
        return new List<CompanyInfo>();

      return new List<CompanyInfo>
      {
        new CompanyInfo()
        {
          AudioDbId = companyId,
          Name = company,
          Type = type,
          Order = 0
        }
      };
    }

    #endregion

    #region Update

    public override async Task<bool> UpdateFromOnlineMusicTrackAlbumPersonAsync(AlbumInfo albumInfo, PersonInfo person, string language, bool cacheOnly)
    {
      try
      {
        AudioDbArtist artistDetail = null;
        language = language ?? PreferredLanguage;

        if (person.AudioDbId > 0)
          artistDetail = await _audioDbHandler.GetArtistAsync(person.AudioDbId, language, cacheOnly).ConfigureAwait(false);
        if (artistDetail == null && !string.IsNullOrEmpty(person.MusicBrainzId))
        {
          List<AudioDbArtist> foundArtists = await _audioDbHandler.GetArtistByMbidAsync(person.MusicBrainzId, language, cacheOnly).ConfigureAwait(false);
          if (foundArtists != null && foundArtists.Count == 1)
          {
            artistDetail = await _audioDbHandler.GetArtistAsync(foundArtists[0].ArtistId, language, cacheOnly).ConfigureAwait(false);
          }
        }
        if (artistDetail == null) return false;

        bool languageSet = artistDetail.SetLanguage(language);

        int? year = artistDetail.BornYear == null ? artistDetail.FormedYear : artistDetail.BornYear;
        DateTime? born = null;
        if (year.HasValue && year.Value > 1900)
          born = new DateTime(year.Value, 1, 1);
        DateTime? died = null;
        if (artistDetail.DiedYear.HasValue && artistDetail.DiedYear.Value > 1900)
          died = new DateTime(artistDetail.DiedYear.Value, 1, 1);

        person.MusicBrainzId = artistDetail.MusicBrainzID;
        person.Name = artistDetail.Artist;
        person.AlternateName = artistDetail.ArtistAlternate;
        person.Biography = new SimpleTitle(artistDetail.Biography, !languageSet);
        person.DateOfBirth = born;
        person.DateOfDeath = died;
        person.Orign = artistDetail.Country;
        person.IsGroup = artistDetail.Members.HasValue ? artistDetail.Members.Value > 1 : false;
        person.Occupation = PersonAspect.OCCUPATION_ARTIST;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheAudioDbWrapper: Exception while processing person {0}", ex, person.ToString());
        return false;
      }
    }

    public override Task<bool> UpdateFromOnlineMusicTrackPersonAsync(TrackInfo trackInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return UpdateFromOnlineMusicTrackAlbumPersonAsync(trackInfo.CloneBasicInstance<AlbumInfo>(), person, language, cacheOnly);
    }

    public override async Task<bool> UpdateFromOnlineMusicTrackAsync(TrackInfo track, string language, bool cacheOnly)
    {
      try
      {
        bool cacheIncomplete = false;
        AudioDbTrack trackDetail = null;
        language = language ?? PreferredLanguage;

        if (track.AudioDbId > 0)
          trackDetail = await _audioDbHandler.GetTrackAsync(track.AudioDbId, language, cacheOnly).ConfigureAwait(false);
        //Musicbrainz Id can be unreliable in this regard because it is linked to a recording and the same recording can
        //be across multiple different albums. In other words the Id is unique per song not per album song, so using this can
        //lead to the wrong album id.
        //if (trackDetail == null && !string.IsNullOrEmpty(track.MusicBrainzId))
        //  trackDetail = _audioDbHandler.GetTrackByMbid(track.MusicBrainzId, language, cacheOnly);
        if (trackDetail == null && track.TrackNum > 0 && (track.AlbumAudioDbId > 0 || !string.IsNullOrEmpty(track.AlbumMusicBrainzGroupId)))
        {
          List<AudioDbTrack> foundTracks = await _audioDbHandler.GetTracksByAlbumIdAsync(track.AlbumAudioDbId, language, cacheOnly).ConfigureAwait(false);
          if (foundTracks == null && !string.IsNullOrEmpty(track.AlbumMusicBrainzGroupId))
          {
            List<AudioDbAlbum> foundAlbums = await _audioDbHandler.GetAlbumByMbidAsync(track.AlbumMusicBrainzGroupId, language, cacheOnly).ConfigureAwait(false);
            if (foundAlbums != null && foundAlbums.Count == 1)
            {
              var albumDetail = await _audioDbHandler.GetAlbumAsync(foundAlbums[0].AlbumId, language, cacheOnly).ConfigureAwait(false);
              if (albumDetail != null)
                foundTracks = await _audioDbHandler.GetTracksByAlbumIdAsync(albumDetail.AlbumId, language, cacheOnly).ConfigureAwait(false);
            }
          }

          if (foundTracks != null && foundTracks.Count > 0)
            trackDetail = foundTracks.FirstOrDefault(t => t.TrackNumber == track.TrackNum);
        }
        if (trackDetail == null) return false;

        //Get the track into the cache
        AudioDbTrack trackTempDetail = await _audioDbHandler.GetTrackAsync(trackDetail.TrackID, language, cacheOnly).ConfigureAwait(false);
        if (trackTempDetail != null)
          trackDetail = trackTempDetail;

        track.AudioDbId = trackDetail.TrackID;
        track.LyricId = trackDetail.LyricID.HasValue ? trackDetail.LyricID.Value : 0;
        track.MvDbId = trackDetail.MvDbID.HasValue ? trackDetail.MvDbID.Value : 0;
        track.MusicBrainzId = trackDetail.MusicBrainzID;
        track.AlbumAudioDbId = trackDetail.AlbumID.HasValue ? trackDetail.AlbumID.Value : 0;
        track.AlbumMusicBrainzGroupId = trackDetail.MusicBrainzAlbumID;

        track.TrackName = trackDetail.Track;
        track.Album = trackDetail.Album;
        track.TrackNum = trackDetail.TrackNumber;
        track.DiscNum = trackDetail.CD.HasValue ? trackDetail.CD.Value : 1;
        track.Rating = new SimpleRating(trackDetail.Rating, trackDetail.RatingCount);
        track.TrackLyrics = trackDetail.TrackLyrics;
        track.Duration = trackDetail.Duration.HasValue ? trackDetail.Duration.Value / 1000 : 0;

        if (trackDetail.ArtistID.HasValue)
        {
          track.Artists = ConvertToPersons(trackDetail.ArtistID.Value, trackDetail.MusicBrainzArtistID, trackDetail.Artist, PersonAspect.OCCUPATION_ARTIST, trackDetail.Track, trackDetail.Album);
          track.AlbumArtists = ConvertToPersons(trackDetail.ArtistID.Value, trackDetail.MusicBrainzArtistID, trackDetail.Artist, PersonAspect.OCCUPATION_ARTIST, trackDetail.Track, trackDetail.Album);
        }

        if (!string.IsNullOrEmpty(trackDetail.Genre?.Trim()))
          track.Genres.Add(new GenreInfo { Name = trackDetail.Genre.Trim() });

        if (trackDetail.AlbumID.HasValue)
        {
          AudioDbAlbum album = await _audioDbHandler.GetAlbumAsync(trackDetail.AlbumID.Value, language, cacheOnly).ConfigureAwait(false);
          if (cacheOnly && album == null)
            cacheIncomplete = true;
          if (album != null && album.LabelId.HasValue)
            track.MusicLabels = ConvertToCompanies(album.LabelId.Value, album.Label, CompanyAspect.COMPANY_MUSIC_LABEL);
        }

        return !cacheIncomplete;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheAudioDbWrapper: Exception while processing track {0}", ex, track.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineMusicTrackAlbumAsync(AlbumInfo album, string language, bool cacheOnly)
    {
      try
      {
        bool cacheIncomplete = false;
        AudioDbAlbum albumDetail = null;
        language = language ?? PreferredLanguage;

        if (album.AudioDbId > 0)
          albumDetail = await _audioDbHandler.GetAlbumAsync(album.AudioDbId, language, cacheOnly).ConfigureAwait(false);
        if (albumDetail == null && !string.IsNullOrEmpty(album.MusicBrainzGroupId))
        {
          List<AudioDbAlbum> foundAlbums = await _audioDbHandler.GetAlbumByMbidAsync(album.MusicBrainzGroupId, language, cacheOnly).ConfigureAwait(false);
          if (foundAlbums != null && foundAlbums.Count == 1)
          {
            albumDetail = await _audioDbHandler.GetAlbumAsync(foundAlbums[0].AlbumId, language, cacheOnly).ConfigureAwait(false);
          }
        }
        if (albumDetail == null) return false;

        bool languageSet = albumDetail.SetLanguage(language);

        album.AudioDbId = albumDetail.AlbumId;
        album.MusicBrainzGroupId = albumDetail.MusicBrainzID;
        album.AmazonId = albumDetail.AmazonID;
        album.ItunesId = albumDetail.ItunesID;

        album.Album = albumDetail.Album;
        album.Description = new SimpleTitle(albumDetail.Description, !languageSet);

        if (!string.IsNullOrEmpty(albumDetail.Genre?.Trim()))
          album.Genres.Add(new GenreInfo { Name = albumDetail.Genre.Trim() });

        album.Sales = albumDetail.Sales ?? 0;
        album.ReleaseDate = albumDetail.Year.HasValue && albumDetail.Year.Value > 1900 ? new DateTime(albumDetail.Year.Value, 1, 1) : default(DateTime?);
        album.Rating = new SimpleRating(albumDetail.Rating, albumDetail.RatingCount);

        if (albumDetail.ArtistId.HasValue)
          album.Artists = ConvertToPersons(albumDetail.ArtistId.Value, albumDetail.MusicBrainzArtistID, albumDetail.Artist, PersonAspect.OCCUPATION_ARTIST, null, albumDetail.Album);

        if (albumDetail.LabelId.HasValue)
          album.MusicLabels = ConvertToCompanies(albumDetail.LabelId.Value, albumDetail.Label, CompanyAspect.COMPANY_MUSIC_LABEL);

        List<AudioDbTrack> albumTracks = await _audioDbHandler.GetTracksByAlbumIdAsync(albumDetail.AlbumId, language, cacheOnly).ConfigureAwait(false);
        if (cacheOnly && albumTracks == null)
          cacheIncomplete = true;
        if (albumTracks != null && albumTracks.Count > 0)
        {
          album.TotalTracks = albumTracks.Count;

          foreach (AudioDbTrack trackDetail in albumTracks)
          {
            TrackInfo track = new TrackInfo();
            track.AudioDbId = trackDetail.TrackID;
            track.LyricId = trackDetail.LyricID.HasValue ? trackDetail.LyricID.Value : 0;
            track.MvDbId = trackDetail.MvDbID.HasValue ? trackDetail.MvDbID.Value : 0;
            track.MusicBrainzId = trackDetail.MusicBrainzID;
            track.AlbumAudioDbId = trackDetail.AlbumID.HasValue ? trackDetail.AlbumID.Value : 0;
            track.AlbumMusicBrainzGroupId = trackDetail.MusicBrainzAlbumID;
            track.AlbumAmazonId = albumDetail.AmazonID;
            track.AlbumItunesId = albumDetail.ItunesID;

            track.TrackName = trackDetail.Track;
            track.Album = albumDetail.Album;
            track.TrackNum = trackDetail.TrackNumber;
            track.DiscNum = trackDetail.CD.HasValue ? trackDetail.CD.Value : 1;
            track.Rating = new SimpleRating(trackDetail.Rating, trackDetail.RatingCount);
            track.TrackLyrics = trackDetail.TrackLyrics;
            track.Duration = trackDetail.Duration.HasValue ? trackDetail.Duration.Value / 1000 : 0;

            if (trackDetail.ArtistID.HasValue)
              track.Artists = ConvertToPersons(trackDetail.ArtistID.Value, trackDetail.MusicBrainzArtistID, trackDetail.Artist, PersonAspect.OCCUPATION_ARTIST, trackDetail.Track, albumDetail.Album);

            if (!string.IsNullOrEmpty(trackDetail.Genre?.Trim()))
              track.Genres.Add(new GenreInfo { Name = trackDetail.Genre.Trim() });

            track.AlbumArtists = album.Artists.ToList();
            track.MusicLabels = album.MusicLabels.ToList();

            album.Tracks.Add(track);
          }
        }

        return !cacheIncomplete;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheAudioDbWrapper: Exception while processing album {0}", ex, album.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineMusicTrackAlbumCompanyAsync(AlbumInfo album, CompanyInfo company, string language, bool cacheOnly)
    {
      try
      {
        AudioDbAlbum albumDetail = null;
        language = language ?? PreferredLanguage;

        if (album.AudioDbId > 0)
          albumDetail = await _audioDbHandler.GetAlbumAsync(album.AudioDbId, language, cacheOnly).ConfigureAwait(false);
        if (albumDetail == null && !string.IsNullOrEmpty(album.MusicBrainzId))
        {
          List<AudioDbAlbum> foundAlbums = await _audioDbHandler.GetAlbumByMbidAsync(album.MusicBrainzId, language, cacheOnly).ConfigureAwait(false);
          if (foundAlbums != null && foundAlbums.Count == 1)
          {
            //Get the album into the cache
            albumDetail = await _audioDbHandler.GetAlbumAsync(foundAlbums[0].AlbumId, language, cacheOnly).ConfigureAwait(false);
          }
        }
        if (albumDetail == null) return false;

        if (!string.IsNullOrEmpty(albumDetail.Label) && company.MatchNames(company.Name, albumDetail.Label) && albumDetail.LabelId.HasValue)
        {
          company.AudioDbId = albumDetail.LabelId.Value;
          company.Name = albumDetail.Label;
          company.Type = CompanyAspect.COMPANY_MUSIC_LABEL;
          return true;
        }

        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheAudioDbWrapper: Exception while processing company {0}", ex, company.ToString());
        return false;
      }
    }

    #endregion

    #region FanArt

    public override Task<ApiWrapperImageCollection<string>> GetFanArtAsync<T>(T infoObject, string language, string fanartMediaType)
    {
      if (fanartMediaType == FanArtMediaTypes.Album)
        return GetAlbumFanArtAsync(infoObject.AsAlbum(), language);
      if (fanartMediaType == FanArtMediaTypes.Artist)
        return GetArtistFanArtAsync(infoObject as PersonInfo, language);
      return Task.FromResult<ApiWrapperImageCollection<string>>(null);
    }

    public override Task<bool> DownloadFanArtAsync(string id, string image, string folderPath)
    {
      int intId;
      return int.TryParse(id, out intId) ? _audioDbHandler.DownloadImageAsync(intId, image, folderPath) : Task.FromResult(false);
    }

    protected async Task<ApiWrapperImageCollection<string>> GetAlbumFanArtAsync(AlbumInfo album, string language)
    {
      if (album == null || album.AudioDbId < 1)
        return null;
      AudioDbAlbum albumDetail = await _audioDbHandler.GetAlbumAsync(album.AudioDbId, language, false).ConfigureAwait(false);
      if (albumDetail == null)
        return null;
      ApiWrapperImageCollection<string> images = new ApiWrapperImageCollection<string>();
      images.Id = album.AudioDbId.ToString();
      if (!string.IsNullOrEmpty(albumDetail.AlbumThumb)) images.Covers.Add(albumDetail.AlbumThumb);
      if (!string.IsNullOrEmpty(albumDetail.AlbumCDart)) images.DiscArt.Add(albumDetail.AlbumCDart);
      return images;
    }

    protected async Task<ApiWrapperImageCollection<string>> GetArtistFanArtAsync(PersonInfo person, string language)
    {
      if (person == null || person.AudioDbId < 1)
        return null;
      AudioDbArtist artistDetail = await _audioDbHandler.GetArtistAsync(person.AudioDbId, language, false).ConfigureAwait(false);
      if (artistDetail == null)
        return null;
      ApiWrapperImageCollection<string> images = new ApiWrapperImageCollection<string>();
      images.Id = person.AudioDbId.ToString();
      if (!string.IsNullOrEmpty(artistDetail.ArtistBanner)) images.Banners.Add(artistDetail.ArtistBanner);
      if (!string.IsNullOrEmpty(artistDetail.ArtistFanart)) images.Backdrops.Add(artistDetail.ArtistFanart);
      if (!string.IsNullOrEmpty(artistDetail.ArtistFanart2)) images.Backdrops.Add(artistDetail.ArtistFanart2);
      if (!string.IsNullOrEmpty(artistDetail.ArtistFanart3)) images.Backdrops.Add(artistDetail.ArtistFanart3);
      if (!string.IsNullOrEmpty(artistDetail.ArtistLogo)) images.Logos.Add(artistDetail.ArtistLogo);
      if (!string.IsNullOrEmpty(artistDetail.ArtistThumb)) images.Thumbnails.Add(artistDetail.ArtistThumb);
      return images;
    }

    #endregion
  }
}
