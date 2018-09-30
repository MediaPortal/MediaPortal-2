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
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class MusicBrainzWrapper : ApiWrapper<TrackImage, string>
  {
    protected MusicBrainzApiV2 _musicBrainzHandler;

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath, bool useHttps)
    {
      _musicBrainzHandler = new MusicBrainzApiV2(cachePath, useHttps);
      SetDefaultLanguage(MusicBrainzApiV2.DefaultLanguage);
      SetRegionLanguages(new List<string> { "XW", "XE" }); //Worldwide and european wide
      SetCachePath(cachePath);
      return true;
    }

    #region Search

    public override async Task<List<TrackInfo>> SearchTrackAsync(TrackInfo trackSearch, string language)
    {
      language = language ?? PreferredLanguage;

      List<TrackResult> foundTracks = await _musicBrainzHandler.SearchTrackAsync(trackSearch.TrackName, trackSearch.Artists.Select(a => a.Name).ToList(),
        trackSearch.Album, trackSearch.ReleaseDate.HasValue ? trackSearch.ReleaseDate.Value.Year : default(int?),
        trackSearch.TrackNum > 0 ? trackSearch.TrackNum : default(int?)).ConfigureAwait(false);
      if (foundTracks == null && trackSearch.AlbumArtists.Count > 0)
      {
        foundTracks = await _musicBrainzHandler.SearchTrackAsync(trackSearch.TrackName, trackSearch.AlbumArtists.Select(a => a.Name).ToList(),
          trackSearch.Album, trackSearch.ReleaseDate.HasValue ? trackSearch.ReleaseDate.Value.Year : default(int?),
          trackSearch.TrackNum > 0 ? trackSearch.TrackNum : default(int?)).ConfigureAwait(false);
      }
      if (foundTracks == null) return null;

      List<TrackInfo> tracks = null;
      foreach (TrackResult track in foundTracks)
      {
        if (tracks == null)
          tracks = new List<TrackInfo>();

        TrackInfo info = new TrackInfo()
        {
          MusicBrainzId = track.Id,
          AlbumAmazonId = track.AlbumAmazonId,
          AlbumMusicBrainzId = track.AlbumId,
          TrackName = track.Title,
          ReleaseDate = track.ReleaseDate,
          TrackNum = track.TrackNum,
          Artists = ConvertToPersons(track.Artists, PersonAspect.OCCUPATION_ARTIST),
          Album = track.Album,
          Compilation = track.FromCompilation,
          AlbumHasBarcode = !string.IsNullOrEmpty(track.AlbumBarcode),
          AlbumHasOnlineCover = track.AlbumHasCover
        };
        info.Languages.Add(track.Country);
        tracks.Add(info);
      }

      return tracks;
    }

    public override async Task<List<AlbumInfo>> SearchTrackAlbumAsync(AlbumInfo albumSearch, string language)
    {
      language = language ?? PreferredLanguage;

      List<TrackRelease> foundReleases = await _musicBrainzHandler.SearchReleaseAsync(albumSearch.Album, albumSearch.Artists.Select(a => a.Name).ToList(),
        albumSearch.ReleaseDate.HasValue ? albumSearch.ReleaseDate.Value.Year : default(int?), albumSearch.TotalTracks > 0 ? albumSearch.TotalTracks : default(int?)).ConfigureAwait(false);
      if (foundReleases == null) return null;

      List<AlbumInfo> albums = null;
      foreach (TrackRelease album in foundReleases)
      {
        if (albums == null)
          albums = new List<AlbumInfo>();

        AlbumInfo info = new AlbumInfo()
        {
          MusicBrainzId = album.Id,
          AmazonId = album.AmazonId,
          MusicBrainzGroupId = album.ReleaseGroup != null ? album.ReleaseGroup.Id : null,
          Album = album.Title,
          ReleaseDate = album.Date,
          TotalTracks = album.TrackCount,
          Artists = ConvertToPersons(album.Artists, PersonAspect.OCCUPATION_ARTIST),
          MusicLabels = ConvertToCompanies(album.Labels, CompanyAspect.COMPANY_MUSIC_LABEL),
          HasOnlineCover = album.CoverArt != null && album.CoverArt.Front ? true : false,
          HasBarcode = !string.IsNullOrEmpty(album.Barcode),
        };
        info.Languages.Add(album.Country);
        albums.Add(info);
      }

      return albums;
    }

    public override async Task<List<PersonInfo>> SearchPersonAsync(PersonInfo personSearch, string language)
    {
      language = language ?? PreferredLanguage;

      if (personSearch.Occupation != PersonAspect.OCCUPATION_ARTIST)
        return null;

      List<TrackArtist> foundArtists = await _musicBrainzHandler.SearchArtistAsync(personSearch.Name).ConfigureAwait(false);
      if (foundArtists == null) return null;

      List<PersonInfo> persons = null;
      foreach (TrackArtist artist in foundArtists)
      {
        if (persons == null)
          persons = new List<PersonInfo>();

        PersonInfo info = new PersonInfo()
        {
          MusicBrainzId = artist.Id,
          Name = artist.Name,
          Occupation = PersonAspect.OCCUPATION_ARTIST,
          IsGroup = string.IsNullOrEmpty(artist.Type) ? false : artist.Type.IndexOf("Group", StringComparison.InvariantCultureIgnoreCase) >= 0,
        };
        persons.Add(info);
      }

      return persons;
    }

    public override async Task<List<CompanyInfo>> SearchCompanyAsync(CompanyInfo companySearch, string language)
    {
      language = language ?? PreferredLanguage;

      if (companySearch.Type != CompanyAspect.COMPANY_MUSIC_LABEL)
        return null;

      List<TrackLabelSearchResult> foundLabels = await _musicBrainzHandler.SearchLabelAsync(companySearch.Name).ConfigureAwait(false);
      if (foundLabels == null) return null;

      List<CompanyInfo> companies = null;
      foreach (TrackLabelSearchResult company in foundLabels)
      {
        if (companies == null)
          companies = new List<CompanyInfo>();

        CompanyInfo info = new CompanyInfo()
        {
          MusicBrainzId = company.Id,
          Name = company.Name,
          Type = CompanyAspect.COMPANY_MUSIC_LABEL,
        };
        companies.Add(info);
      }

      return companies;
    }

    #endregion

    #region Convert

    private List<PersonInfo> ConvertToPersons(List<TrackArtistCredit> artists, string occupation)
    {
      return ConvertToPersons(artists.Select(a => a.Artist).ToList(), occupation);
    }

    private List<PersonInfo> ConvertToPersons(List<TrackBaseName> artists, string occupation)
    {
      if (artists == null || artists.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (TrackBaseName person in artists)
      {
        retValue.Add(new PersonInfo
        {
          MusicBrainzId = person.Id,
          Name = person.Name,
          Occupation = occupation,
          Order = sortOrder++
        });
      }
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<string> artists, string occupation)
    {
      if (artists == null || artists.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (string person in artists)
      {
        retValue.Add(new PersonInfo
        {
          Name = person,
          Occupation = occupation,
          Order = sortOrder++
        });
      }
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanies(List<TrackLabel> companies, string type)
    {
      if (companies == null || companies.Count == 0)
        return new List<CompanyInfo>();

      int sortOrder = 0;
      List<CompanyInfo> retValue = new List<CompanyInfo>();
      foreach (TrackLabel label in companies)
        retValue.Add(new CompanyInfo() { MusicBrainzId = label.Label.Id, Name = label.Label.Name, Type = type, Order = sortOrder++ });
      return retValue;
    }

    #endregion

    #region Update

    public override async Task<bool> UpdateFromOnlineMusicTrackAlbumCompanyAsync(AlbumInfo album, CompanyInfo company, string language, bool cacheOnly)
    {
      try
      {
        TrackLabel labelDetail = null;
        if (!string.IsNullOrEmpty(company.MusicBrainzId))
          labelDetail = await _musicBrainzHandler.GetLabelAsync(company.MusicBrainzId, cacheOnly).ConfigureAwait(false);
        if (labelDetail == null) return false;
        if (labelDetail.Label == null) return false;

        company.MusicBrainzId = labelDetail.Label.Id;
        company.Name = labelDetail.Label.Name;
        company.Type = CompanyAspect.COMPANY_MUSIC_LABEL;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzWrapper: Exception while processing company {0}", ex, company.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineMusicTrackAlbumPersonAsync(AlbumInfo albumInfo, PersonInfo person, string language, bool cacheOnly)
    {
      try
      {
        TrackArtist artistDetail = null;
        if (!string.IsNullOrEmpty(person.MusicBrainzId))
          artistDetail = await _musicBrainzHandler.GetArtistAsync(person.MusicBrainzId, cacheOnly).ConfigureAwait(false);
        if (artistDetail == null) return false;

        person.Name = artistDetail.Name;
        person.DateOfBirth = artistDetail.LifeSpan != null ? artistDetail.LifeSpan.Begin : null;
        person.DateOfDeath = artistDetail.LifeSpan != null ? artistDetail.LifeSpan.End : null;
        person.IsGroup = string.IsNullOrEmpty(artistDetail.Type) ? false : artistDetail.Type.IndexOf("Group", StringComparison.InvariantCultureIgnoreCase) >= 0;
        person.Occupation = PersonAspect.OCCUPATION_ARTIST;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzWrapper: Exception while processing person {0}", ex, person.ToString());
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
        Track trackDetail = null;
        if (!string.IsNullOrEmpty(track.MusicBrainzId))
          trackDetail = await _musicBrainzHandler.GetTrackAsync(track.MusicBrainzId, cacheOnly).ConfigureAwait(false);

        if (trackDetail == null && !cacheOnly && !string.IsNullOrEmpty(track.IsrcId))
        {
          List<TrackResult> foundTracks = await _musicBrainzHandler.SearchTrackFromIsrcAsync(track.IsrcId).ConfigureAwait(false);
          if (foundTracks != null && foundTracks.Count == 1)
          {
            trackDetail = await _musicBrainzHandler.GetTrackAsync(foundTracks[0].Id, cacheOnly).ConfigureAwait(false);
          }
        }
        if (trackDetail == null) return false;

        if (!string.IsNullOrEmpty(trackDetail.AlbumId))
        {
          trackDetail.InitPropertiesFromAlbum(trackDetail.AlbumId, null, null);
        }
        else if (!string.IsNullOrEmpty(trackDetail.Album))
        {
          if (!trackDetail.InitPropertiesFromAlbum(null, trackDetail.Album, PreferredLanguage))
          {
            if (!trackDetail.InitPropertiesFromAlbum(null, trackDetail.Album, "XW")) //World releases
            {
              if (!trackDetail.InitPropertiesFromAlbum(null, trackDetail.Album, "XE")) //European releases
              {
                if (DefaultLanguage != PreferredLanguage) //Try US releases
                  trackDetail.InitPropertiesFromAlbum(null, trackDetail.Album, DefaultLanguage);
              }
            }
          }
        }

        track.MusicBrainzId = trackDetail.Id;
        track.AlbumMusicBrainzId = trackDetail.AlbumId;
        track.AlbumAmazonId = trackDetail.AlbumAmazonId;

        track.TrackName = trackDetail.Title;
        track.Album = trackDetail.Album;
        track.ReleaseDate = trackDetail.ReleaseDate;
        track.TrackNum = trackDetail.TrackNum;
        track.TotalTracks = trackDetail.TotalTracks;
        track.DiscNum = trackDetail.DiscId;
        track.Rating = new SimpleRating(trackDetail.RatingValue, trackDetail.RatingVotes);

        track.Artists = ConvertToPersons(trackDetail.TrackArtists, PersonAspect.OCCUPATION_ARTIST);
        track.Composers = ConvertToPersons(trackDetail.Composers, PersonAspect.OCCUPATION_COMPOSER);
        track.AlbumArtists = ConvertToPersons(trackDetail.AlbumArtists, PersonAspect.OCCUPATION_ARTIST);
        //Tags are not really good as genre
        //track.Genres = trackDetail.TagValues;
        track.IsrcId = trackDetail.Isrcs != null && trackDetail.Isrcs.Count == 1 ? trackDetail.Isrcs[0] : null;

        //Try to find album from group
        if (string.IsNullOrEmpty(track.AlbumMusicBrainzId) && !string.IsNullOrEmpty(track.AlbumMusicBrainzGroupId))
        {
          TrackReleaseGroup trackReleaseGroup = await _musicBrainzHandler.GetReleaseGroupAsync(track.AlbumMusicBrainzGroupId, cacheOnly).ConfigureAwait(false);
          if (trackReleaseGroup != null && !string.IsNullOrEmpty(track.Album))
          {
            if (!trackReleaseGroup.InitPropertiesFromAlbum(null, track.Album, PreferredLanguage))
            {
              if (!trackReleaseGroup.InitPropertiesFromAlbum(null, track.Album, "XW")) //World releases
              {
                if (trackReleaseGroup.InitPropertiesFromAlbum(null, track.Album, "XE")) //European releases
                {
                  if (DefaultLanguage != PreferredLanguage) //Try US releases
                    trackReleaseGroup.InitPropertiesFromAlbum(null, trackDetail.Album, DefaultLanguage);
                }
              }
            }
            if (!string.IsNullOrEmpty(trackReleaseGroup.AlbumId))
            {
              track.AlbumMusicBrainzId = trackReleaseGroup.AlbumId;
            }
          }
        }

        if (!string.IsNullOrEmpty(track.AlbumMusicBrainzId))
        {
          TrackRelease trackRelease = await _musicBrainzHandler.GetAlbumAsync(track.AlbumMusicBrainzId, cacheOnly).ConfigureAwait(false);
          if (trackRelease != null)
          {
            track.AlbumMusicBrainzGroupId = trackRelease.ReleaseGroup != null ? trackRelease.ReleaseGroup.Id : null;
            track.MusicLabels = ConvertToCompanies(trackRelease.Labels, CompanyAspect.COMPANY_MUSIC_LABEL);
          }
        }

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzWrapper: Exception while processing track {0}", ex, track.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineMusicTrackAlbumAsync(AlbumInfo album, string language, bool cacheOnly)
    {
      try
      {
        TrackRelease albumDetail = null;
        if (!string.IsNullOrEmpty(album.MusicBrainzId))
          albumDetail = await _musicBrainzHandler.GetAlbumAsync(album.MusicBrainzId, cacheOnly).ConfigureAwait(false);
        if (albumDetail == null) return false;

        album.MusicBrainzId = albumDetail.Id;
        album.AmazonId = albumDetail.AmazonId;
        album.MusicBrainzGroupId = albumDetail.ReleaseGroup != null ? albumDetail.ReleaseGroup.Id : null;

        album.Album = albumDetail.Title;
        album.TotalTracks = albumDetail.TrackCount;
        album.ReleaseDate = albumDetail.Date;

        album.Artists = ConvertToPersons(albumDetail.Artists, PersonAspect.OCCUPATION_ARTIST);

        if (albumDetail.Labels != null)
          album.MusicLabels = ConvertToCompanies(albumDetail.Labels, CompanyAspect.COMPANY_MUSIC_LABEL);

        foreach (TrackMedia media in albumDetail.Media)
        {
          if (media.Tracks == null || media.Tracks.Count <= 0)
            continue;

          if (media.Tracks.Count == album.TotalTracks || album.TotalTracks == 0)
          {
            foreach (TrackData trackDetail in media.Tracks)
            {
              TrackInfo track = new TrackInfo();
              track.MusicBrainzId = trackDetail.Id;
              track.AlbumMusicBrainzId = albumDetail.Id;

              track.TrackName = trackDetail.Title;
              track.Album = albumDetail.Title;
              track.TotalTracks = album.TotalTracks;
              track.DiscNum = album.DiscNum;
              track.ReleaseDate = album.ReleaseDate;

              int trackNo = 0;
              if (int.TryParse(trackDetail.Number, out trackNo))
                track.TrackNum = trackNo;

              track.Artists = ConvertToPersons(trackDetail.Artists, PersonAspect.OCCUPATION_ARTIST);
              track.AlbumArtists = ConvertToPersons(albumDetail.Artists, PersonAspect.OCCUPATION_ARTIST);

              album.Tracks.Add(track);
            }
          }
          return true;
        }

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzWrapper: Exception while processing album {0}", ex, album.ToString());
        return false;
      }
    }

    #endregion

    #region FanArt

    public override Task<ApiWrapperImageCollection<TrackImage>> GetFanArtAsync<T>(T infoObject, string language, string fanartMediaType)
    {
      if (fanartMediaType == FanArtMediaTypes.Album)
        return GetAlbumFanArtAsync(infoObject.AsAlbum());
      return Task.FromResult<ApiWrapperImageCollection<TrackImage>>(null);
    }

    public override Task<bool> DownloadFanArtAsync(string id, TrackImage image, string folderPath)
    {
      return !string.IsNullOrEmpty(id) ? _musicBrainzHandler.DownloadImageAsync(id, image, folderPath) : Task.FromResult(false);
    }

    protected async Task<ApiWrapperImageCollection<TrackImage>> GetAlbumFanArtAsync(AlbumInfo album)
    {
      if (album == null || string.IsNullOrEmpty(album.MusicBrainzId))
        return null;
      // Download all image information, filter later!
      TrackImageCollection albumImages = await _musicBrainzHandler.GetImagesAsync(album.MusicBrainzId).ConfigureAwait(false);
      if (albumImages == null)
        return null;
      ApiWrapperImageCollection<TrackImage> images = new ApiWrapperImageCollection<TrackImage>();
      images.Id = album.MusicBrainzId;
      images.Covers.AddRange(albumImages.Images);
      return images;
    }

    #endregion
  }
}
