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
using System.Linq;
using System.Collections.Generic;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.FanArt;

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

    public override bool SearchTrack(TrackInfo trackSearch, string language, out List<TrackInfo> tracks)
    {
      tracks = null;
      language = language ?? PreferredLanguage;

      List<TrackResult> foundTracks = _musicBrainzHandler.SearchTrack(trackSearch.TrackName, trackSearch.Artists.Select(a => a.Name).ToList(),
        trackSearch.Album, trackSearch.ReleaseDate.HasValue ? trackSearch.ReleaseDate.Value.Year : default(int?),
        trackSearch.TrackNum > 0 ? trackSearch.TrackNum : default(int?));
      if (foundTracks == null && trackSearch.AlbumArtists.Count > 0)
      {
        foundTracks = _musicBrainzHandler.SearchTrack(trackSearch.TrackName, trackSearch.AlbumArtists.Select(a => a.Name).ToList(),
          trackSearch.Album, trackSearch.ReleaseDate.HasValue ? trackSearch.ReleaseDate.Value.Year : default(int?),
          trackSearch.TrackNum > 0 ? trackSearch.TrackNum : default(int?));
      }
      if (foundTracks == null) return false;

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

      return tracks != null;
    }

    public override bool SearchTrackAlbum(AlbumInfo albumSearch, string language, out List<AlbumInfo> albums)
    {
      albums = null;
      language = language ?? PreferredLanguage;

      List<TrackRelease> foundReleases = _musicBrainzHandler.SearchRelease(albumSearch.Album, albumSearch.Artists.Select(a => a.Name).ToList(),
        albumSearch.ReleaseDate.HasValue ? albumSearch.ReleaseDate.Value.Year : default(int?), albumSearch.TotalTracks > 0 ? albumSearch.TotalTracks : default(int?));
      if (foundReleases == null) return false;

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

      return albums != null;
    }

    public override bool SearchPerson(PersonInfo personSearch, string language, out List<PersonInfo> persons)
    {
      persons = null;
      language = language ?? PreferredLanguage;

      if (personSearch.Occupation != PersonAspect.OCCUPATION_ARTIST)
        return false;

      List<TrackArtist> foundArtists = _musicBrainzHandler.SearchArtist(personSearch.Name);
      if (foundArtists == null) return false;

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

      return persons != null;
    }

    public override bool SearchCompany(CompanyInfo companySearch, string language, out List<CompanyInfo> companies)
    {
      companies = null;
      language = language ?? PreferredLanguage;

      if (companySearch.Type != CompanyAspect.COMPANY_MUSIC_LABEL)
        return false;

      List<TrackLabelSearchResult> foundLabels = _musicBrainzHandler.SearchLabel(companySearch.Name);
      if (foundLabels == null) return false;

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

      return companies != null;
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

    public override bool UpdateFromOnlineMusicTrackAlbumCompany(AlbumInfo album, CompanyInfo company, string language, bool cacheOnly)
    {
      try
      {
        TrackLabel labelDetail = null;
        if (!string.IsNullOrEmpty(company.MusicBrainzId))
          labelDetail = _musicBrainzHandler.GetLabel(company.MusicBrainzId, cacheOnly);
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

    public override bool UpdateFromOnlineMusicTrackAlbumPerson(AlbumInfo albumInfo, PersonInfo person, string language, bool cacheOnly)
    {
      try
      {
        TrackArtist artistDetail = null;
        if (!string.IsNullOrEmpty(person.MusicBrainzId))
          artistDetail = _musicBrainzHandler.GetArtist(person.MusicBrainzId, cacheOnly);
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

    public override bool UpdateFromOnlineMusicTrackPerson(TrackInfo trackInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return UpdateFromOnlineMusicTrackAlbumPerson(trackInfo.CloneBasicInstance<AlbumInfo>(), person, language, cacheOnly);
    }

    public override bool UpdateFromOnlineMusicTrack(TrackInfo track, string language, bool cacheOnly)
    {
      try
      {
        Track trackDetail = null;
        if (!string.IsNullOrEmpty(track.MusicBrainzId))
          trackDetail = _musicBrainzHandler.GetTrack(track.MusicBrainzId, cacheOnly);

        if (trackDetail == null && !cacheOnly && !string.IsNullOrEmpty(track.IsrcId))
        {
          List<TrackResult> foundTracks = _musicBrainzHandler.SearchTrackFromIsrc(track.IsrcId);
          if (foundTracks != null && foundTracks.Count == 1)
          {
            trackDetail = _musicBrainzHandler.GetTrack(foundTracks[0].Id, cacheOnly);
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
          TrackReleaseGroup trackReleaseGroup = _musicBrainzHandler.GetReleaseGroup(track.AlbumMusicBrainzGroupId, cacheOnly);
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
          TrackRelease trackRelease = _musicBrainzHandler.GetAlbum(track.AlbumMusicBrainzId, cacheOnly);
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

    public override bool UpdateFromOnlineMusicTrackAlbum(AlbumInfo album, string language, bool cacheOnly)
    {
      try
      {
        TrackRelease albumDetail = null;
        if (!string.IsNullOrEmpty(album.MusicBrainzId))
          albumDetail = _musicBrainzHandler.GetAlbum(album.MusicBrainzId, cacheOnly);
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

    public override bool GetFanArt<T>(T infoObject, string language, string fanartMediaType, out ApiWrapperImageCollection<TrackImage> images)
    {
      images = new ApiWrapperImageCollection<TrackImage>();

      try
      {
        if (fanartMediaType == FanArtMediaTypes.Album)
        {
          TrackInfo track = infoObject as TrackInfo;
          AlbumInfo album = infoObject as AlbumInfo;
          if (album == null && track != null)
          {
            album = track.CloneBasicInstance<AlbumInfo>();
          }
          if (album != null && !string.IsNullOrEmpty(album.MusicBrainzId))
          {
            // Download all image information, filter later!
            TrackImageCollection albumImages = _musicBrainzHandler.GetImages(album.MusicBrainzId);
            if (albumImages != null)
            {
              images.Id = album.MusicBrainzId;
              images.Covers.AddRange(albumImages.Images);
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

    public override bool DownloadFanArt(string id, TrackImage image, string folderPath)
    {
      if (!string.IsNullOrEmpty(id))
      {
        return _musicBrainzHandler.DownloadImage(id, image, folderPath);
      }
      return false;
    }

    #endregion
  }
}
