#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.MusicBrainz;
using System.IO;
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class MusicBrainzMatcher : BaseMatcher<TrackMatch, string>
  {
    #region Static instance

    public static MusicBrainzMatcher Instance
    {
      get { return ServiceRegistration.Get<MusicBrainzMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\MusicBrainz\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, Track> _memoryCache = new ConcurrentDictionary<string, Track>(StringComparer.OrdinalIgnoreCase);
    private MusicBrainzWrapper _musicBrainzDb;

    #endregion

    #region Metadata updaters

    public bool FindAndUpdateTrack(TrackInfo trackInfo)
    {
      try
      {
        Track trackDetails;
        if (TryMatch(trackInfo, out trackDetails))
        {
          string mbid = null;
          if (trackDetails != null)
          {
            mbid = trackDetails.Id;

            MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumMusicBrainzId, trackDetails.AlbumId);
            MetadataUpdater.SetOrUpdateId(ref trackInfo.MusicBrainzId, trackDetails.Id);

            MetadataUpdater.SetOrUpdateString(ref trackInfo.TrackName, trackDetails.Title, true);
            MetadataUpdater.SetOrUpdateString(ref trackInfo.Album, trackDetails.Album, true);
            MetadataUpdater.SetOrUpdateValue(ref trackInfo.ReleaseDate, trackDetails.ReleaseDate);
            MetadataUpdater.SetOrUpdateValue(ref trackInfo.TrackNum, trackDetails.TrackNum);
            MetadataUpdater.SetOrUpdateValue(ref trackInfo.TotalTracks, trackDetails.TotalTracks);
            MetadataUpdater.SetOrUpdateValue(ref trackInfo.DiscNum, trackDetails.DiscId);
            MetadataUpdater.SetOrUpdateRatings(ref trackInfo.TotalRating, ref trackInfo.RatingCount, trackDetails.RatingValue, trackDetails.RatingVotes);

            MetadataUpdater.SetOrUpdateList(trackInfo.Artists, ConvertToPersons(trackDetails.TrackArtists, PersonAspect.OCCUPATION_ARTIST), true, true);
            MetadataUpdater.SetOrUpdateList(trackInfo.Composers, ConvertToPersons(trackDetails.Composers, PersonAspect.OCCUPATION_COMPOSER), true, true);
            MetadataUpdater.SetOrUpdateList(trackInfo.AlbumArtists, ConvertToPersons(trackDetails.AlbumArtists, PersonAspect.OCCUPATION_ARTIST), true, true);
            //Tags are not really good as genre
            //MetadataUpdater.SetOrUpdateList(trackInfo.Genres, trackDetails.TagValues, false);

            //Try to find album from group
            TrackReleaseGroup trackReleaseGroup;
            if (string.IsNullOrEmpty(trackInfo.AlbumMusicBrainzId) && !string.IsNullOrEmpty(trackInfo.AlbumMusicBrainzGroupId) && 
              _musicBrainzDb.GetReleaseGroup(trackInfo.MusicBrainzId, out trackReleaseGroup))
            {
              if (!string.IsNullOrEmpty(trackInfo.Album))
              {
                if (!trackReleaseGroup.InitPropertiesFromAlbum(null, trackInfo.Album, _musicBrainzDb.PreferredLanguage))
                {
                  if (!trackReleaseGroup.InitPropertiesFromAlbum(null, trackInfo.Album, "XW")) //World releases
                  {
                    trackReleaseGroup.InitPropertiesFromAlbum(null, trackInfo.Album, "XE"); //European releases
                  }
                }
                if (!string.IsNullOrEmpty(trackReleaseGroup.AlbumId))
                {
                  MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumMusicBrainzId, trackReleaseGroup.AlbumId);
                }
              }
            }

            TrackRelease trackRelease;
            if (!string.IsNullOrEmpty(trackInfo.AlbumMusicBrainzId) && _musicBrainzDb.GetAlbum(trackInfo.AlbumMusicBrainzId, out trackRelease))
            {
              MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumMusicBrainzGroupId, trackRelease.ReleaseGroup != null ? trackRelease.ReleaseGroup.Id : null);

              MetadataUpdater.SetOrUpdateList(trackInfo.MusicLabels, ConvertToCompanies(trackRelease.Labels, CompanyAspect.COMPANY_MUSIC_LABEL), true, true);

              if (trackInfo.Thumbnail == null)
              {
                List<string> thumbs = GetFanArtFiles(trackInfo, FanArtScope.Album, FanArtType.Covers);
                if (thumbs.Count > 0)
                  trackInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
              }
            }
          }

          if (!string.IsNullOrEmpty(mbid))
            ScheduleDownload(mbid);
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Exception while processing track {0}", ex, trackInfo.ToString());
        return false;
      }
    }

    public bool UpdateAlbum(AlbumInfo albumInfo)
    {
      try
      {
        TrackRelease trackRelease;
        if (!string.IsNullOrEmpty(albumInfo.MusicBrainzId) && _musicBrainzDb.GetAlbum(albumInfo.MusicBrainzId, out trackRelease))
        {
          MetadataUpdater.SetOrUpdateId(ref albumInfo.MusicBrainzGroupId, trackRelease.ReleaseGroup != null ? trackRelease.ReleaseGroup.Id : null);

          MetadataUpdater.SetOrUpdateString(ref albumInfo.Album, trackRelease.Title, true);
          MetadataUpdater.SetOrUpdateValue(ref albumInfo.TotalTracks, trackRelease.TrackCount);
          MetadataUpdater.SetOrUpdateValue(ref albumInfo.ReleaseDate, trackRelease.Date);

          MetadataUpdater.SetOrUpdateList(albumInfo.Artists, ConvertToPersons(trackRelease.Artists, PersonAspect.OCCUPATION_ARTIST), true, false);

          if (trackRelease.Labels != null)
            MetadataUpdater.SetOrUpdateList(albumInfo.MusicLabels, ConvertToCompanies(trackRelease.Labels, CompanyAspect.COMPANY_MUSIC_LABEL), true, false);

          if (albumInfo.Thumbnail == null)
          {
            List<string> thumbs = GetFanArtFiles(albumInfo, FanArtScope.Album, FanArtType.Covers);
            if (thumbs.Count > 0)
              albumInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Exception while processing album {0}", ex, albumInfo.ToString());
        return false;
      }
    }

    public bool UpdateAlbumPersons(AlbumInfo albumInfo, string occupation)
    {
      return UpdatePersons(albumInfo.Artists, occupation);
    }

    public bool UpdateTrackPersons(TrackInfo trackInfo, string occupation)
    {
      return UpdatePersons(trackInfo.Artists, occupation);
    }

    private bool UpdatePersons(List<PersonInfo> persons, string occupation)
    {
      try
      {
        if (occupation != PersonAspect.OCCUPATION_ARTIST)
          return false;

        // Try online lookup
        if (!Init())
          return false;

        TrackArtist artistDetail;
        int sortOrder = 0;
        foreach (PersonInfo person in persons)
        {
          if(string.IsNullOrEmpty(person.MusicBrainzId))
          {
            List<TrackArtist> artists;
            if (_musicBrainzDb.SearchArtistUnique(person.Name, out artists))
              person.MusicBrainzId = artists[0].Id;
          }
          if (!string.IsNullOrEmpty(person.MusicBrainzId) && _musicBrainzDb.GetArtist(person.MusicBrainzId, out artistDetail))
          {
            person.Name = artistDetail.Name;
            person.DateOfBirth = artistDetail.LifeSpan != null ? artistDetail.LifeSpan.Begin : null;
            person.DateOfDeath = artistDetail.LifeSpan != null ? artistDetail.LifeSpan.End : null;
            person.IsGroup = string.IsNullOrEmpty(artistDetail.Type) ? false : artistDetail.Type.IndexOf("Group", StringComparison.InvariantCultureIgnoreCase) >= 0;
            person.Occupation = occupation;
            person.Order = sortOrder++;
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Exception while processing persons", ex);
        return false;
      }
    }

    public bool UpdateAlbumCompanies(AlbumInfo albumInfo, string type)
    {
      try
      {
        if (type != CompanyAspect.COMPANY_MUSIC_LABEL)
          return false;

        // Try online lookup
        if (!Init())
          return false;

        foreach (CompanyInfo company in albumInfo.MusicLabels)
        {
          if (string.IsNullOrEmpty(company.MusicBrainzId))
          {
            List<TrackLabelSearchResult> labels;
            if (_musicBrainzDb.SearchLabelUnique(company.Name, out labels))
              company.MusicBrainzId = labels[0].Id;
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Exception while processing companies", ex);
        return false;
      }
    }

    #endregion

    #region Metadata update helpers

    private List<PersonInfo> ConvertToPersons(List<TrackArtistCredit> artist, string occupation)
    {
      return ConvertToPersons(artist.Select(a => a.Artist).ToList(), occupation);
    }

    private List<PersonInfo> ConvertToPersons(List<TrackBaseName> artist, string occupation)
    {
      if (artist == null || artist.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (TrackBaseName person in artist)
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

    private void StoreTrackMatch(Track track, TrackInfo searchTrack)
    {
      if (track == null)
      {
        _storage.TryAddMatch(new TrackMatch()
        {
          ItemName = searchTrack.ToString()
        });
        return;
      }
      TrackInfo trackMatch = new TrackInfo()
      {
        Album = track.Album,
        TrackNum = track.TrackNum,
        TrackName = track.Title
      };
      var onlineMatch = new TrackMatch
      {
        Id = track.Id,
        ItemName = searchTrack.ToString(),
        TrackName = trackMatch.ToString(),
        TrackNum = track.TrackNum,
        ArtistName = track.Artists != null && track.Artists.Count > 0 ? track.Artists[0].Name : null,
        AlbumName = track.Album
      };
      _storage.TryAddMatch(onlineMatch);
    }

    #endregion

    #region Online matching

    protected bool TryMatch(TrackInfo trackInfo, out Track trackDetails)
    {
      //Try to find release from track
      if (!string.IsNullOrEmpty(trackInfo.MusicBrainzId) && _musicBrainzDb.GetTrack(trackInfo.MusicBrainzId, out trackDetails))
      {
        if (!string.IsNullOrEmpty(trackInfo.AlbumMusicBrainzId))
        {
          trackDetails.InitPropertiesFromAlbum(trackInfo.AlbumMusicBrainzId, null, null);
        }
        else if (!string.IsNullOrEmpty(trackInfo.Album))
        {
          if(!trackDetails.InitPropertiesFromAlbum(null, trackInfo.Album, _musicBrainzDb.PreferredLanguage))
          {
            if (!trackDetails.InitPropertiesFromAlbum(null, trackInfo.Album, "XW")) //World releases
            {
              trackDetails.InitPropertiesFromAlbum(null, trackInfo.Album, "XE"); //European releases
            }
          }
        }

        StoreTrackMatch(trackDetails, trackInfo);
        return true;
      }
      trackDetails = null;
      return TryMatch(trackInfo.TrackName, trackInfo.Artists.Select(a => a.Name).ToList(), trackInfo.Album,
          trackInfo.ReleaseDate.HasValue ? trackInfo.ReleaseDate.Value.Year : 0, trackInfo.TrackNum, false, out trackDetails);
    }

    protected bool TryMatch(string title, List<string> artists, string album, int year, int trackNum, bool cacheOnly, out Track trackDetail)
    {
      TrackInfo searchTrack = new TrackInfo()
      {
        Album = album,
        TrackNum = trackNum,
        ReleaseDate = year > 0 ? new DateTime(year, 1, 1) : default(DateTime?),
        TrackName = title
      };
      trackDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(searchTrack.ToString(), out trackDetail))
          return true;

        // Load cache or create new list
        List<TrackMatch> matches = _storage.GetMatches();

        // Init empty
        trackDetail = null;

        TrackMatch match = matches.Find(m =>
          (string.Equals(m.ItemName, searchTrack.ToString(), StringComparison.OrdinalIgnoreCase) || string.Equals(m.TrackName, searchTrack.ToString(), StringComparison.OrdinalIgnoreCase)) &&
          !string.IsNullOrEmpty(m.AlbumName) && album.Length > 0 ? album.Equals(m.AlbumName, StringComparison.OrdinalIgnoreCase) : true &&
          ((trackNum > 0 && m.TrackNum > 0 && int.Equals(m.TrackNum, trackNum) || trackNum <= 0 || m.TrackNum <= 0)));
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Try to lookup track \"{0}\" from cache: {1}", title, match != null && string.IsNullOrEmpty(match.Id) == false);

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known track, only return the track details.
        if (match != null)
          return !string.IsNullOrEmpty(match.Id) && _musicBrainzDb.GetTrack(match.Id, out trackDetail);

        if (cacheOnly)
          return false;

        List<TrackResult> tracks;
        if (_musicBrainzDb.SearchTrackUnique(title, artists, album, year, trackNum, out tracks))
        {
          TrackResult trackResult = tracks[0];
          ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Found unique online match for \"{0}\": \"{1}\"", title, trackResult.Title);
          if (_musicBrainzDb.GetTrack(trackResult.Id, out trackDetail))
          {
            if (!string.IsNullOrEmpty(trackResult.AlbumId))
            {
              trackDetail.InitPropertiesFromAlbum(trackResult.AlbumId, null, null);
            }
            else if (!string.IsNullOrEmpty(trackResult.Album))
            {
              if (!trackDetail.InitPropertiesFromAlbum(null, trackResult.Album, _musicBrainzDb.PreferredLanguage))
              {
                if (!trackDetail.InitPropertiesFromAlbum(null, trackResult.Album, "XW")) //World releases
                {
                  trackDetail.InitPropertiesFromAlbum(null, trackResult.Album, "XE"); //European releases
                }
              }
            }
          }
          StoreTrackMatch(trackDetail, searchTrack);
          return true;
        }
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: No unique match found for \"{0}\"", title);
        // Also save "non matches" to avoid retrying
        StoreTrackMatch(null, searchTrack);
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Exception while processing track {0}", ex, title);
        return false;
      }
      finally
      {
        if (trackDetail != null)
          _memoryCache.TryAdd(searchTrack.ToString(), trackDetail);
      }
    }

    #endregion

    #region Caching

    /// <summary>
    /// Check if the memory cache should be cleared and starts an online update of (file-) cached series information.
    /// </summary>
    private void CheckCacheAndRefresh()
    {
      if (DateTime.Now - _memoryCacheInvalidated <= MAX_MEMCACHE_DURATION)
        return;
      _memoryCache.Clear();
      _memoryCacheInvalidated = DateTime.Now;

      // TODO: when updating track information is implemented, start here a job to do it
    }

    #endregion

    public override bool Init()
    {
      if (!base.Init())
        return false;

      if (_musicBrainzDb != null)
        return true;

      _musicBrainzDb = new MusicBrainzWrapper();
      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      string lang = currentCulture.Name;
      if (lang.Contains("-")) lang = lang.Split('-')[1];
      _musicBrainzDb.SetPreferredLanguage(lang);
      return _musicBrainzDb.Init(CACHE_PATH);
    }

    #region FanArt

    public List<string> GetFanArtFiles<T>(T infoObject, string scope, string type)
    {
      List<string> fanartFiles = new List<string>();
      string path = null;
      if (scope == FanArtScope.Album)
      {
        AlbumInfo album = infoObject as AlbumInfo;
        TrackInfo track = infoObject as TrackInfo;
        if (album != null && !string.IsNullOrEmpty(album.MusicBrainzId))
        {
          path = Path.Combine(CACHE_PATH, album.MusicBrainzId, string.Format(@"{0}\{1}\", scope, type));
        }
        else if (track != null && !string.IsNullOrEmpty(track.AlbumMusicBrainzId))
        {
          path = Path.Combine(CACHE_PATH, track.AlbumMusicBrainzId, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      if (Directory.Exists(path))
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
      return fanartFiles;
    }

    protected override void DownloadFanArt(string musicBrainzId)
    {
      try
      {
        if (string.IsNullOrEmpty(musicBrainzId))
          return;

        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher Download: Started for ID {0}", musicBrainzId);

        if (!Init())
          return;

        Track track;
        if (!_musicBrainzDb.GetTrack(musicBrainzId, out track))
          return;

        if(string.IsNullOrEmpty(track.AlbumId))
        {
          //No fanart
          FinishDownloadFanArt(musicBrainzId);
          return;
        }

        TrackImageCollection imageCollection;
        if (!_musicBrainzDb.GetAlbumFanArt(track.AlbumId, out imageCollection))
          return;

        // Save Cover and CDArt
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher Download: Begin saving fanarts for ID {0}", musicBrainzId);
        DownloadImages(track.AlbumId, imageCollection, "Front", string.Format(@"{0}\{1}", FanArtScope.Album, FanArtType.Covers));
        DownloadImages(track.AlbumId, imageCollection, "Medium", string.Format(@"{0}\{1}", FanArtScope.Album, FanArtType.DiscArt));
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher Download: Finished ID {0}", musicBrainzId);

        // Remember we are finished
        FinishDownloadFanArt(musicBrainzId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzMatcher: Exception downloading FanArt for ID {0}", ex, musicBrainzId);
      }
    }

    private int DownloadImages(string albumId, TrackImageCollection imageCollection, string type, string category)
    {
      if (imageCollection == null) return 0;

      int idx = 0;
      foreach (TrackImage image in imageCollection.Images)
      {
        if (idx >= MAX_FANART_IMAGES)
          break;

        foreach (string imageType in image.Types)
        {
          if (imageType.Equals(type, StringComparison.InvariantCultureIgnoreCase))
          {
            if(_musicBrainzDb.DownloadImage(albumId, image, category))
              idx++;
            break;
          }
        }
      }
      return idx;
    }

    #endregion
  }
}
