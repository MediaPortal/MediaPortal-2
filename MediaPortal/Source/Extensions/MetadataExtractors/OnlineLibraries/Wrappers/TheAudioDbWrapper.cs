#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Linq;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Common.Logging;
using MediaPortal.Common;

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

    public override bool SearchTrack(TrackInfo trackSearch, string language, out List<TrackInfo> tracks)
    {
      tracks = null;
      language = language ?? PreferredLanguage;

      List<AudioDbTrack> foundTracks = null;
      foreach(PersonInfo person in trackSearch.Artists)
      {
        foundTracks = _audioDbHandler.SearchTrack(person.Name, trackSearch.TrackName);
        if (foundTracks != null)
          break;
      }
      if (foundTracks == null)
      {
        foreach (PersonInfo person in trackSearch.AlbumArtists)
        {
          foundTracks = _audioDbHandler.SearchTrack(person.Name, trackSearch.TrackName);
          if (foundTracks != null)
            break;
        }
      }
      if (foundTracks == null) return false;

      foreach (AudioDbTrack track in foundTracks)
      {
        if (tracks == null)
          tracks = new List<TrackInfo>();

        track.SetLanguage(language);

        TrackInfo info = new TrackInfo()
        {
          AudioDbId = track.TrackID,
          AlbumAudioDbId = track.AlbumID ?? 0,
          MusicBrainzId = track.MusicBrainzID,
          AlbumMusicBrainzGroupId = track.MusicBrainzAlbumID,
          
          TrackName = track.Track,
          TrackNum = track.TrackNumber,
          Artists = ConvertToPersons(track.ArtistID ?? 0, track.MusicBrainzArtistID, track.Artist, PersonAspect.OCCUPATION_ARTIST),
          Album = track.Album,
        };
        tracks.Add(info);
      }

      return tracks != null;
    }

    public override bool SearchTrackAlbum(AlbumInfo albumSearch, string language, out List<AlbumInfo> albums)
    {
      albums = null;
      language = language ?? PreferredLanguage;

      List<AudioDbAlbum> foundAlbums = _audioDbHandler.SearchAlbum(albumSearch.Artists.Count > 0 ? albumSearch.Artists[0].Name : "", albumSearch.Album);
      if (foundAlbums == null) return false;

      foreach (AudioDbAlbum album in foundAlbums)
      {
        if (albums == null)
          albums = new List<AlbumInfo>();

        album.SetLanguage(language);

        AlbumInfo info = new AlbumInfo()
        {
          AudioDbId = album.AlbumId,
          MusicBrainzGroupId = album.MusicBrainzID,
          
          Album = album.Album,
          ReleaseDate = album.Year != null && album.Year.Value > 1900 ? new DateTime(album.Year.Value, 1, 1) : default(DateTime?),
          Artists = ConvertToPersons(album.ArtistId ?? 0, album.MusicBrainzArtistID, album.Artist, PersonAspect.OCCUPATION_ARTIST),
          MusicLabels = ConvertToCompanies(album.LabelId ?? 0, album.Label, CompanyAspect.COMPANY_MUSIC_LABEL),
        };
        albums.Add(info);
      }

      return albums != null;
    }

    public override bool SearchPerson(PersonInfo personSearch, string language, out List<PersonInfo> persons)
    {
      persons = null;
      language = language ?? PreferredLanguage;

      if (personSearch.Occupation != PersonAspect.OCCUPATION_ARTIST)
        return false;

      List<AudioDbArtist> foundArtists = _audioDbHandler.SearchArtist(personSearch.Name);
      if (foundArtists == null) return false;

      foreach (AudioDbArtist artist in foundArtists)
      {
        if (persons == null)
          persons = new List<PersonInfo>();

        artist.SetLanguage(language);

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

      return persons != null;
    }

    #endregion

    #region Convert

    private List<PersonInfo> ConvertToPersons(long artistId, string mbArtistId, string artist, string occupation)
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
          Order = sortOrder++
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

    public override bool UpdateFromOnlineMusicTrackAlbumPerson(AlbumInfo albumInfo, PersonInfo person, string language, bool cacheOnly)
    {
      AudioDbArtist artistDetail = null;
      language = language ?? PreferredLanguage;

      if (person.AudioDbId > 0)
        artistDetail = _audioDbHandler.GetArtist(person.AudioDbId, cacheOnly);
      if (artistDetail == null && !string.IsNullOrEmpty(person.MusicBrainzId))
      {
        List<AudioDbArtist> foundArtists = _audioDbHandler.GetArtistByMbid(person.MusicBrainzId, cacheOnly);
        if (foundArtists != null && foundArtists.Count == 1)
        {
          //Get the artist into the cache
          artistDetail = _audioDbHandler.GetArtist(foundArtists[0].ArtistId, cacheOnly);
        }
      }
      if (artistDetail == null) return false;

      artistDetail.SetLanguage(language);

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
      person.Biography = artistDetail.Biography;
      person.DateOfBirth = born;
      person.DateOfDeath = died;
      person.Orign = artistDetail.Country;
      person.IsGroup = artistDetail.Members.HasValue ? artistDetail.Members.Value > 1 : false;
      person.Occupation = PersonAspect.OCCUPATION_ARTIST;

      return true;
    }

    public override bool UpdateFromOnlineMusicTrackPerson(TrackInfo trackInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return UpdateFromOnlineMusicTrackAlbumPerson(trackInfo.CloneBasicInstance<AlbumInfo>(), person, language, cacheOnly);
    }

    public override bool UpdateFromOnlineMusicTrack(TrackInfo track, string language, bool cacheOnly)
    {
      bool cacheIncomplete = false;
      AudioDbTrack trackDetail = null;
      language = language ?? PreferredLanguage;

      if (track.AudioDbId > 0)
        trackDetail = _audioDbHandler.GetTrack(track.AudioDbId, cacheOnly);
      if (trackDetail == null && !string.IsNullOrEmpty(track.MusicBrainzId))
      {
        AudioDbTrack foundTrack = _audioDbHandler.GetTrackByMbid(track.MusicBrainzId, cacheOnly);
        if (foundTrack != null)
        {
          //Get the track into the cache
          trackDetail = _audioDbHandler.GetTrack(foundTrack.TrackID, cacheOnly);
        }
      }
      if (trackDetail == null && track.TrackNum > 0 && track.AlbumAudioDbId > 0)
      {
        List<AudioDbTrack> foundTracks = _audioDbHandler.GetTracksByAlbumId(track.AlbumAudioDbId, cacheOnly);
        if (foundTracks != null && foundTracks.Count > 0)
        {
          trackDetail = foundTracks.FirstOrDefault(t => t.TrackNumber == track.TrackNum);
        }
      }
      if (trackDetail == null) return false;

      trackDetail.SetLanguage(language);

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
      track.Duration = trackDetail.Duration ?? 0;

      if (trackDetail.ArtistID.HasValue)
      {
        track.Artists = ConvertToPersons(trackDetail.ArtistID.Value, trackDetail.MusicBrainzArtistID, trackDetail.Artist, PersonAspect.OCCUPATION_ARTIST);
        track.AlbumArtists = ConvertToPersons(trackDetail.ArtistID.Value, trackDetail.MusicBrainzArtistID, trackDetail.Artist, PersonAspect.OCCUPATION_ARTIST);
      }
      track.Genres = new List<string>(new string[] { trackDetail.Genre });

      if (trackDetail.AlbumID.HasValue)
      {
        AudioDbAlbum album = _audioDbHandler.GetAlbum(trackDetail.AlbumID.Value, cacheOnly);
        if (cacheOnly && album == null)
          cacheIncomplete = true;
        if (album != null && album.LabelId.HasValue)
          track.MusicLabels = ConvertToCompanies(album.LabelId.Value, album.Label, CompanyAspect.COMPANY_MUSIC_LABEL);
      }

      return !cacheIncomplete;
    }

    public override bool UpdateFromOnlineMusicTrackAlbum(AlbumInfo album, string language, bool cacheOnly)
    {
      bool cacheIncomplete = false;
      AudioDbAlbum albumDetail = null;
      language = language ?? PreferredLanguage;

      if (album.AudioDbId > 0)
        albumDetail = _audioDbHandler.GetAlbum(album.AudioDbId, cacheOnly);
      if (albumDetail == null && !string.IsNullOrEmpty(album.MusicBrainzId))
      {
        List<AudioDbAlbum> foundAlbums = _audioDbHandler.GetAlbumByMbid(album.MusicBrainzId, cacheOnly);
        if (foundAlbums != null && foundAlbums.Count == 1)
        {
          //Get the album into the cache
          albumDetail = _audioDbHandler.GetAlbum(foundAlbums[0].AlbumId, cacheOnly);
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
      album.Genres = new List<string>(new string[] { albumDetail.Genre });
      album.Sales = albumDetail.Sales ?? 0;
      album.ReleaseDate = albumDetail.Year.HasValue && albumDetail.Year.Value > 1900 ? new DateTime(albumDetail.Year.Value, 1, 1) : default(DateTime?);
      album.Rating = new SimpleRating(albumDetail.Rating, albumDetail.RatingCount);

      if (albumDetail.ArtistId.HasValue)
        album.Artists = ConvertToPersons(albumDetail.ArtistId.Value, albumDetail.MusicBrainzArtistID, albumDetail.Artist, PersonAspect.OCCUPATION_ARTIST);

      if (albumDetail.LabelId.HasValue)
        album.MusicLabels = ConvertToCompanies(albumDetail.LabelId.Value, albumDetail.Label, CompanyAspect.COMPANY_MUSIC_LABEL);

      List<AudioDbTrack> albumTracks = _audioDbHandler.GetTracksByAlbumId(albumDetail.AlbumId, cacheOnly);
      if (cacheOnly && albumTracks == null)
        cacheIncomplete = true;
      if (albumTracks != null && albumTracks.Count > 0)
      {
        album.TotalTracks = albumTracks.Count;

        foreach(AudioDbTrack trackDetail in albumTracks)
        {
          trackDetail.SetLanguage(language);

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
          track.Duration = trackDetail.Duration ?? 0;

          if (trackDetail.ArtistID.HasValue)
          {
            track.Artists = ConvertToPersons(trackDetail.ArtistID.Value, trackDetail.MusicBrainzArtistID, trackDetail.Artist, PersonAspect.OCCUPATION_ARTIST);
          }
          track.Genres = new List<string>(new string[] { trackDetail.Genre });

          track.AlbumArtists = album.Artists;
          track.MusicLabels = album.MusicLabels;

          album.Tracks.Add(track);
        }
      }

      return !cacheIncomplete;
    }

    public override bool UpdateFromOnlineMusicTrackAlbumCompany(AlbumInfo album, CompanyInfo company, string language, bool cacheOnly)
    {
      AudioDbAlbum albumDetail = null;
      language = language ?? PreferredLanguage;

      if (album.AudioDbId > 0)
        albumDetail = _audioDbHandler.GetAlbum(album.AudioDbId, cacheOnly);
      if (albumDetail == null && !string.IsNullOrEmpty(album.MusicBrainzId))
      {
        List<AudioDbAlbum> foundAlbums = _audioDbHandler.GetAlbumByMbid(album.MusicBrainzId, cacheOnly);
        if (foundAlbums != null && foundAlbums.Count == 1)
        {
          //Get the album into the cache
          albumDetail = _audioDbHandler.GetAlbum(foundAlbums[0].AlbumId, cacheOnly);
        }
      }
      if (albumDetail == null) return false;

      if (!string.IsNullOrEmpty(albumDetail.Label) && BaseInfo.MatchNames(company.Name, albumDetail.Label) && albumDetail.LabelId.HasValue)
      {
        company.AudioDbId = albumDetail.LabelId.Value;
        company.Name = albumDetail.Label;
        company.Type = CompanyAspect.COMPANY_MUSIC_LABEL;
        return true;
      }

      return false;
    }

    #endregion

    #region FanArt

    public override bool GetFanArt<T>(T infoObject, string language, string scope, out ApiWrapperImageCollection<string> images)
    {
      images = new ApiWrapperImageCollection<string>();

      try
      {
        if (scope == FanArtMediaTypes.Album)
        {
          TrackInfo track = infoObject as TrackInfo;
          AlbumInfo album = infoObject as AlbumInfo;
          if (album == null && track != null)
          {
            album = track.CloneBasicInstance<AlbumInfo>();
          }
          if (album != null && album.AudioDbId > 0)
          {
            AudioDbAlbum albumDetail = _audioDbHandler.GetAlbum(album.AudioDbId, false);
            if (albumDetail != null)
            {
              images.Id = album.AudioDbId.ToString();
              if (!string.IsNullOrEmpty(albumDetail.AlbumThumb)) images.Covers.Add(albumDetail.AlbumThumb);
              if (!string.IsNullOrEmpty(albumDetail.AlbumCDart)) images.DiscArt.Add(albumDetail.AlbumCDart);
              return true;
            }
          }
        }
        else if (scope == FanArtMediaTypes.Artist)
        {
          PersonInfo person = infoObject as PersonInfo;
          if (person != null && person.AudioDbId > 0)
          {
            AudioDbArtist artistDetail = _audioDbHandler.GetArtist(person.AudioDbId, false);
            if (artistDetail != null)
            {
              images.Id = person.AudioDbId.ToString();
              if (!string.IsNullOrEmpty(artistDetail.ArtistBanner)) images.Banners.Add(artistDetail.ArtistBanner);
              if (!string.IsNullOrEmpty(artistDetail.ArtistFanart)) images.Backdrops.Add(artistDetail.ArtistFanart);
              if (!string.IsNullOrEmpty(artistDetail.ArtistFanart2)) images.Backdrops.Add(artistDetail.ArtistFanart2);
              if (!string.IsNullOrEmpty(artistDetail.ArtistFanart3)) images.Backdrops.Add(artistDetail.ArtistFanart3);
              if (!string.IsNullOrEmpty(artistDetail.ArtistLogo)) images.Logos.Add(artistDetail.ArtistLogo);
              if (!string.IsNullOrEmpty(artistDetail.ArtistThumb)) images.Thumbnails.Add(artistDetail.ArtistThumb);
              return true;
            }
          }
        }
        else
        {
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception downloading images", ex);
      }
      return false;
    }

    public override bool DownloadFanArt(string id, string image, string scope, string type)
    {
      int ID;
      if (int.TryParse(id, out ID))
      {
        string category = string.Format(@"{0}\{1}", scope, type);
        return _audioDbHandler.DownloadImage(ID, image, category);
      }
      return false;
    }

    #endregion
  }
}
