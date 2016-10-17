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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public abstract class MusicMatcher<TImg, TLang> : BaseMatcher<TrackMatch, string>, IMusicMatcher
  {
    public class MusicMatcherSettings
    {
      public string LastRefresh { get; set; }
    }

    #region Init

    public MusicMatcher(string cachePath, TimeSpan maxCacheDuration, bool cacheRefreshable)
    {
      _cachePath = cachePath;
      _matchesSettingsFile = Path.Combine(cachePath, "MusicMatches.xml");
      _maxCacheDuration = maxCacheDuration;
      _id = GetType().Name;
      _cacheRefreshable = cacheRefreshable;

      _artistMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "ArtistMatches.xml"));
      _composerMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "ComposerMatches.xml"));
      _labelMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "LabelMatches.xml"));
      _albumMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "AlbumMatches.xml"));
      _configFile = Path.Combine(cachePath, "MusicConfig.xml");

      Init();
    }

    public override bool Init()
    {
      if (!_enabled)
        return false;

      if (_wrapper != null)
        return true;

      if (!base.Init())
        return false;

      return InitWrapper(UseSecureWebCommunication);
    }

    private void LoadConfig()
    {
      _config = Settings.Load<MusicMatcherSettings>(_configFile);
      if (_config == null)
        _config = new MusicMatcherSettings();
    }

    private void SaveConfig()
    {
      Settings.Save(_configFile, _config);
    }

    public abstract bool InitWrapper(bool useHttps);

    #endregion

    #region Constants

    public static string FANART_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    private DateTime _memoryCacheInvalidated = DateTime.MinValue;
    private ConcurrentDictionary<string, TrackInfo> _memoryCache = new ConcurrentDictionary<string, TrackInfo>(StringComparer.OrdinalIgnoreCase);
    private MusicMatcherSettings _config = new MusicMatcherSettings();
    private string _cachePath;
    private string _matchesSettingsFile;
    private string _configFile;
    private TimeSpan _maxCacheDuration;
    private bool _enabled = true;
    private bool _primary = false;
    private string _id = null;
    private bool _cacheRefreshable;

    private SimpleNameMatcher _artistMatcher;
    private SimpleNameMatcher _composerMatcher;
    private SimpleNameMatcher _labelMatcher;
    private SimpleNameMatcher _albumMatcher;

    /// <summary>
    /// Contains the initialized ApiWrapper.
    /// </summary>
    protected ApiWrapper<TImg, TLang> _wrapper = null;

    #endregion

    #region Properties

    public bool Enabled
    {
      get { return _enabled; }
      set { _enabled = value; }
    }

    public bool Primary
    {
      get { return _primary; }
      set { _primary = value; }
    }

    public string Id
    {
      get { return _id; }
    }

    public bool CacheRefreshable
    {
      get { return _cacheRefreshable; }
    }

    #endregion

    #region External match storage

    public virtual void StoreArtistMatch(PersonInfo person)
    {
      string id;
      if (GetPersonId(person, out id))
        _artistMatcher.StoreNameMatch(id, person.Name, person.Name);
    }

    public virtual void StoreComposerMatch(PersonInfo person)
    {
      string id;
      if (GetPersonId(person, out id))
        _composerMatcher.StoreNameMatch(id, person.Name, person.Name);
    }

    public virtual void StoreMusicLabelMatch(CompanyInfo company)
    {
      string id;
      if (GetCompanyId(company, out id))
        _labelMatcher.StoreNameMatch(id, company.Name, company.Name);
    }

    #endregion

    #region Metadata updaters

    /// <summary>
    /// Tries to lookup the music track online and downloads images.
    /// </summary>
    /// <param name="trackInfo">Track to check</param>
    /// <returns><c>true</c> if successful</returns>
    public virtual bool FindAndUpdateTrack(TrackInfo trackInfo, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TrackInfo trackMatch = null;
        string trackId = null;
        bool matchFound = false;
        TLang language = FindBestMatchingLanguage(trackInfo.Languages);
        if (GetTrackId(trackInfo, out trackId))
        {
          // Prefer memory cache
          CheckCacheAndRefresh();
          if (_memoryCache.TryGetValue(trackId, out trackMatch))
          {
            matchFound = true;
          }
        }

        if (!matchFound)
        {
          // Load cache or create new list
          List<TrackMatch> matches = _storage.GetMatches();

          // Use cached values before doing online query
          TrackMatch match = matches.Find(m =>
            (string.Equals(m.ItemName, GetUniqueTrackName(trackInfo), StringComparison.OrdinalIgnoreCase) || string.Equals(m.TrackName, trackInfo.TrackName, StringComparison.OrdinalIgnoreCase)) &&
            (!string.IsNullOrEmpty(m.ArtistName) && trackInfo.Artists.Count > 0 ? trackInfo.Artists[0].Name.Equals(m.ArtistName, StringComparison.OrdinalIgnoreCase) : true) &&
            (!string.IsNullOrEmpty(m.AlbumName) && !string.IsNullOrEmpty(trackInfo.Album) ? trackInfo.Album.Equals(m.AlbumName, StringComparison.OrdinalIgnoreCase) : true) &&
            ((trackInfo.TrackNum > 0 && m.TrackNum > 0 && int.Equals(m.TrackNum, trackInfo.TrackNum) || trackInfo.TrackNum <= 0 || m.TrackNum <= 0)));
          Logger.Debug(_id + ": Try to lookup track \"{0}\" from cache: {1}", trackInfo, match != null && !string.IsNullOrEmpty(match.Id));

          trackMatch = trackInfo.Clone();
          if (match != null)
          {
            if (!CacheRefreshable)
            {
              //Match was found but cache is still the same
              return false;
            }
            else if (SetTrackId(trackMatch, match.Id))
            {
              //If Id was found in cache the online track info is probably also in the cache
              if (_wrapper.UpdateFromOnlineMusicTrack(trackMatch, language, true))
              {
                Logger.Debug(_id + ": Found track {0} in cache", trackInfo.ToString());
                matchFound = true;
              }
            }
            else if (string.IsNullOrEmpty(trackId))
            {
              //Match was found but with invalid Id probably to avoid a retry
              //No Id is available so online search will probably fail again
              return false;
            }
          }

          if (!matchFound && !forceQuickMode)
          {
            Logger.Debug(_id + ": Search for track {0} online", trackInfo.ToString());

            //Try to update track information from online source if online Ids are present
            if (!_wrapper.UpdateFromOnlineMusicTrack(trackMatch, language, false))
            {
              //Search for the track online and update the Ids if a match is found
              if (_wrapper.SearchTrackUniqueAndUpdate(trackMatch, language))
              {
                //Ids were updated now try to update track information from online source
                if (_wrapper.UpdateFromOnlineMusicTrack(trackMatch, language, false))
                  matchFound = true;
              }
            }
            else
            {
              matchFound = true;
            }
          }
        }

        //Always save match even if none to avoid retries
        if (!forceQuickMode)
          StoreTrackMatch(trackInfo, trackMatch);

        if (matchFound)
        {
          bool albumMatch = false; //Only update album related info if they are equal
          if (ApiWrapper<TImg, TLang>.NamesAreMostlyEqual(trackInfo.CloneBasicInstance<AlbumInfo>(), trackMatch.CloneBasicInstance<AlbumInfo>()))
            albumMatch = true;

          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.AudioDbId, trackMatch.AudioDbId);
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.MusicBrainzId, trackMatch.MusicBrainzId);
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.IsrcId, trackMatch.IsrcId);
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.LyricId, trackMatch.LyricId);
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.MusicIpId, trackMatch.MusicIpId);
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.MvDbId, trackMatch.MvDbId);
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref trackInfo.TrackName, trackMatch.TrackName);
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref trackInfo.TrackLyrics, trackMatch.TrackLyrics);
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref trackInfo.ReleaseDate, trackMatch.ReleaseDate);
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateRatings(ref trackInfo.Rating, trackMatch.Rating);
          if (trackInfo.Genres.Count == 0)
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(trackInfo.Genres, trackMatch.Genres, true);

          if (albumMatch)
          {
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumAudioDbId, trackMatch.AlbumAudioDbId);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumCdDdId, trackMatch.AlbumCdDdId);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumMusicBrainzDiscId, trackMatch.AlbumMusicBrainzDiscId);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumMusicBrainzGroupId, trackMatch.AlbumMusicBrainzGroupId);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumMusicBrainzId, trackMatch.AlbumMusicBrainzId);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumAmazonId, trackMatch.AlbumAmazonId);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumItunesId, trackMatch.AlbumItunesId);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref trackInfo.AlbumUpcEanId, trackMatch.AlbumUpcEanId);

            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref trackInfo.Album, trackMatch.Album);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref trackInfo.DiscNum, trackMatch.DiscNum);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref trackInfo.TotalDiscs, trackMatch.TotalDiscs);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref trackInfo.TotalTracks, trackMatch.TotalTracks);
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref trackInfo.TrackNum, trackMatch.TrackNum);
          }

          //These lists contain Ids and other properties that are not persisted, so they will always appear changed.
          //So changes to these lists will only be stored if something else has changed.
          MetadataUpdater.SetOrUpdateList(trackInfo.Artists, trackMatch.Artists, trackInfo.Artists.Count == 0);
          MetadataUpdater.SetOrUpdateList(trackInfo.Composers, trackMatch.Composers, trackInfo.Composers.Count == 0);
          if (albumMatch)
          {
            MetadataUpdater.SetOrUpdateList(trackInfo.MusicLabels, trackMatch.MusicLabels, trackInfo.MusicLabels.Count == 0);
            //In some cases the album artists can be "Various Artist" and/or "Multiple Artists" or other variations
            MetadataUpdater.SetOrUpdateList(trackInfo.AlbumArtists, trackMatch.AlbumArtists, trackInfo.AlbumArtists.Count == 0);
          }

          //Store person matches
          foreach (PersonInfo person in trackInfo.AlbumArtists)
          {
            string id;
            if (GetPersonId(person, out id))
              _artistMatcher.StoreNameMatch(id, person.Name, person.Name);
          }
          foreach (PersonInfo person in trackInfo.Artists)
          {
            string id;
            if (GetPersonId(person, out id))
              _artistMatcher.StoreNameMatch(id, person.Name, person.Name);
          }
          foreach (PersonInfo person in trackInfo.Composers)
          {
            string id;
            if (GetPersonId(person, out id))
              _composerMatcher.StoreNameMatch(id, person.Name, person.Name);
          }

          //Store company matches
          foreach (CompanyInfo company in trackInfo.MusicLabels)
          {
            string id;
            if (GetCompanyId(company, out id))
              _labelMatcher.StoreNameMatch(id, company.Name, company.Name);
          }

          if (GetTrackId(trackInfo, out trackId))
          {
            _memoryCache.TryAdd(trackId, trackInfo);
          }

          return true;
        }

        return false;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing track {0}", ex, trackInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateTrackPersons(TrackInfo trackInfo, string occupation, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(trackInfo.Languages);
        bool updated = false;
        TrackInfo trackMatch = trackInfo.Clone();
        List<PersonInfo> persons = new List<PersonInfo>();
        if (occupation == PersonAspect.OCCUPATION_ARTIST)
        {
          foreach (PersonInfo person in trackMatch.Artists)
          {
            string id;
            if (_artistMatcher.GetNameMatch(person.Name, out id))
            {
              if (SetPersonId(person, id))
              {
                //Only add if Id valid if not then it is to avoid a retry
                //and the person should be ignored
                persons.Add(person);
                updated = true;
              }
            }
            else
            {
              persons.Add(person);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_COMPOSER)
        {
          foreach (PersonInfo person in trackMatch.Composers)
          {
            string id;
            if (_composerMatcher.GetNameMatch(person.Name, out id))
            {
              if (SetPersonId(person, id))
              {
                //Only add if Id valid if not then it is to avoid a retry
                //and the person should be ignored
                persons.Add(person);
                updated = true;
              }
            }
            else
            {
              persons.Add(person);
            }
          }
        }
        foreach (PersonInfo person in persons)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineMusicTrackPerson(trackMatch, person, language, true))
          {
            if (!forceQuickMode)
            {
              Logger.Debug(_id + ": Search for person {0} online", person.ToString());

              //Try to update person information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineMusicTrackPerson(trackMatch, person, language, false))
              {
                //Search for the person online and update the Ids if a match is found
                if (_wrapper.SearchPersonUniqueAndUpdate(person, language))
                {
                  //Ids were updated now try to fetch the online person info
                  if (_wrapper.UpdateFromOnlineMusicTrackPerson(trackMatch, person, language, false))
                  {
                    //Set as changed because cache has changed and might contain new/updated data
                    trackInfo.HasChanged = true;
                    updated = true;
                  }
                }
              }
              else
              {
                updated = true;
              }
            }
          }
          else
          {
            Logger.Debug(_id + ": Found person {0} in cache", person.ToString());
            updated = true;
          }
        }

        if (updated == false && occupation == PersonAspect.OCCUPATION_ARTIST)
        {
          //Try to update artist based on album information
          AlbumInfo album = trackMatch.CloneBasicInstance<AlbumInfo>();
          album.Artists = trackMatch.Artists;
          if (UpdateAlbumPersons(album, occupation, forceQuickMode))
            updated = true;
        }

        if (updated)
        {
          //These lists contain Ids and other properties that are not loaded, so they will always appear changed.
          //So these changes will be ignored and only stored if there is any other reason for it to have changed.
          if (occupation == PersonAspect.OCCUPATION_ARTIST)
            MetadataUpdater.SetOrUpdateList(trackInfo.Artists, trackMatch.Artists, false);
          else if (occupation == PersonAspect.OCCUPATION_COMPOSER)
            MetadataUpdater.SetOrUpdateList(trackInfo.Composers, trackMatch.Composers, false);
        }

        List<string> thumbs = new List<string>();
        if (occupation == PersonAspect.OCCUPATION_ARTIST)
        {
          foreach (PersonInfo person in trackInfo.Artists)
          {
            string id;
            if (GetPersonId(person, out id))
            {
              _artistMatcher.StoreNameMatch(id, person.Name, person.Name);
            }
            else
            {
              //Store empty match so he/she is not retried
              if (!forceQuickMode)
                _artistMatcher.StoreNameMatch("", person.Name, person.Name);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_COMPOSER)
        {
          foreach (PersonInfo person in trackInfo.Composers)
          {
            string id;
            if (GetPersonId(person, out id))
            {
              _composerMatcher.StoreNameMatch(id, person.Name, person.Name);
            }
            else
            {
              //Store empty match so he/she is not retried
              _composerMatcher.StoreNameMatch("", person.Name, person.Name);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing persons {0}", ex, trackInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateAlbumPersons(AlbumInfo albumInfo, string occupation, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(albumInfo.Languages);
        bool updated = false;
        AlbumInfo albumMatch = albumInfo.Clone();
        List<PersonInfo> persons = new List<PersonInfo>();
        if (occupation == PersonAspect.OCCUPATION_ARTIST)
        {
          foreach (PersonInfo person in albumMatch.Artists)
          {
            string id;
            if (_artistMatcher.GetNameMatch(person.Name, out id))
            {
              if (SetPersonId(person, id))
              {
                //Only add if Id valid if not then it is to avoid a retry
                //and the person should be ignored
                persons.Add(person);
                updated = true;
              }
            }
            else
            {
              persons.Add(person);
            }
          }
        }
        foreach (PersonInfo person in persons)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineMusicTrackAlbumPerson(albumMatch, person, language, true))
          {
            if (!forceQuickMode)
            {
              Logger.Debug(_id + ": Search for person {0} online", person.ToString());

              //Try to update person information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineMusicTrackAlbumPerson(albumMatch, person, language, false))
              {
                //Search for the person online and update the Ids if a match is found
                if (_wrapper.SearchPersonUniqueAndUpdate(person, language))
                {
                  //Ids were updated now try to fetch the online person info
                  if (_wrapper.UpdateFromOnlineMusicTrackAlbumPerson(albumMatch, person, language, false))
                  {
                    //Set as changed because cache has changed and might contain new/updated data
                    albumInfo.HasChanged = true;
                    updated = true;
                  }
                }
              }
              else
              {
                updated = true;
              }
            }
          }
          else
          {
            Logger.Debug(_id + ": Found person {0} in cache", person.ToString());
            updated = true;
          }
        }

        if (updated)
        {
          //These lists contain Ids and other properties that are not loaded, so they will always appear changed.
          //So these changes will be ignored and only stored if there is any other reason for it to have changed.
          if (occupation == PersonAspect.OCCUPATION_ARTIST)
            MetadataUpdater.SetOrUpdateList(albumInfo.Artists, albumMatch.Artists, false);
        }

        List<string> thumbs = new List<string>();
        if (occupation == PersonAspect.OCCUPATION_ARTIST)
        {
          foreach (PersonInfo person in albumInfo.Artists)
          {
            string id;
            if (GetPersonId(person, out id))
            {
              _artistMatcher.StoreNameMatch(id, person.Name, person.Name);
            }
            else
            {
              //Store empty match so he/she is not retried
              if (!forceQuickMode)
                _artistMatcher.StoreNameMatch("", person.Name, person.Name);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing persons {0}", ex, albumInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateAlbumCompanies(AlbumInfo albumInfo, string companyType, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(albumInfo.Languages);
        bool updated = false;
        AlbumInfo albumMatch = albumInfo.Clone();
        List<CompanyInfo> companies = new List<CompanyInfo>();
        if (companyType == CompanyAspect.COMPANY_MUSIC_LABEL)
        {
          foreach (CompanyInfo company in albumMatch.MusicLabels)
          {
            string id;
            if (_labelMatcher.GetNameMatch(company.Name, out id))
            {
              if (SetCompanyId(company, id))
              {
                //Only add if Id valid if not then it is to avoid a retry
                //and the company should be ignored
                companies.Add(company);
                updated = true;
              }
            }
            else
            {
              companies.Add(company);
            }
          }
        }
        foreach (CompanyInfo company in companies)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineMusicTrackAlbumCompany(albumMatch, company, language, true))
          {
            if (!forceQuickMode)
            {
              Logger.Debug(_id + ": Search for company {0} online", company.ToString());

              //Try to update company information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineMusicTrackAlbumCompany(albumMatch, company, language, false))
              {
                //Search for the company online and update the Ids if a match is found
                if (_wrapper.SearchCompanyUniqueAndUpdate(company, language))
                {
                  //Ids were updated now try to fetch the online company info
                  if (_wrapper.UpdateFromOnlineMusicTrackAlbumCompany(albumMatch, company, language, false))
                  {
                    //Set track as changed because cache has changed and might contain new/updated data
                    albumInfo.HasChanged = true;
                    updated = true;
                  }
                }
              }
              else
              {
                updated = true;
              }
            }
          }
          else
          {
            Logger.Debug(_id + ": Found company {0} in cache", company.ToString());
            updated = true;
          }
        }

        if (updated)
        {
          //These lists contain Ids and other properties that are not loaded, so they will always appear changed.
          //So these changes will be ignored and only stored if there is any other reason for it to have changed.
          if (companyType == CompanyAspect.COMPANY_MUSIC_LABEL)
            MetadataUpdater.SetOrUpdateList(albumInfo.MusicLabels, albumMatch.MusicLabels, false);
        }

        List<string> thumbs = new List<string>();
        if (companyType == CompanyAspect.COMPANY_MUSIC_LABEL)
        {
          foreach (CompanyInfo company in albumInfo.MusicLabels)
          {
            string id;
            if (GetCompanyId(company, out id))
            {
              _labelMatcher.StoreNameMatch(id, company.Name, company.Name);
            }
            else
            {
              //Store empty match so it is not retried
              if (!forceQuickMode)
                _labelMatcher.StoreNameMatch("", company.Name, company.Name);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing companies {0}", ex, albumInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateAlbum(AlbumInfo albumInfo, bool updateTrackList, bool forceQuickMode)
    {
      try
      {
        if (string.IsNullOrEmpty(albumInfo.Album))
          return false;

        // Try online lookup
        if (!Init())
          return false;

        string id;
        if (!GetTrackAlbumId(albumInfo, out id))
        {
          if (_albumMatcher.GetNameMatch(albumInfo.Album, out id))
          {
            if (!CacheRefreshable)
            {
              //Match was found but cache is still the same
              return false;
            }
            else if (!SetTrackAlbumId(albumInfo, id))
            {
              //Match probably stored with invalid Id to avoid retries. 
              //Searching for this album by name only failed so stop trying.
              return false;
            }
          }
        }

        TLang language = FindBestMatchingLanguage(albumInfo.Languages);
        bool updated = false;
        AlbumInfo albumMatch = albumInfo.Clone();
        albumMatch.Tracks.Clear();
        //Try updating from cache
        if (!_wrapper.UpdateFromOnlineMusicTrackAlbum(albumMatch, language, true))
        {
          if (!forceQuickMode)
          {
            Logger.Debug(_id + ": Search for album {0} online", albumInfo.ToString());

            //Try to update company information from online source if online Ids are present
            if (!_wrapper.UpdateFromOnlineMusicTrackAlbum(albumMatch, language, false))
            {
              //Search for the company online and update the Ids if a match is found
              if (_wrapper.SearchTrackAlbumUniqueAndUpdate(albumMatch, language))
              {
                //Ids were updated now try to fetch the online company info
                if (_wrapper.UpdateFromOnlineMusicTrackAlbum(albumMatch, language, false))
                  updated = true;
              }
            }
            else
            {
              updated = true;
            }
          }
        }
        else
        {
          Logger.Debug(_id + ": Found album {0} in cache", albumInfo.ToString());
          updated = true;
        }

        if (updated)
        {
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref albumInfo.AudioDbId, albumMatch.AudioDbId);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref albumInfo.CdDdId, albumMatch.CdDdId);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref albumInfo.MusicBrainzDiscId, albumMatch.MusicBrainzDiscId);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref albumInfo.MusicBrainzGroupId, albumMatch.MusicBrainzGroupId);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref albumInfo.MusicBrainzId, albumMatch.MusicBrainzId);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref albumInfo.AmazonId, albumMatch.AmazonId);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref albumInfo.ItunesId, albumMatch.ItunesId);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref albumInfo.UpcEanId, albumMatch.UpcEanId);

          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref albumInfo.Album, albumMatch.Album);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref albumInfo.Description, albumMatch.Description);

          if (albumInfo.TotalTracks < albumMatch.TotalTracks)
            albumInfo.HasChanged = true;
          MetadataUpdater.SetOrUpdateValue(ref albumInfo.TotalTracks, albumMatch.TotalTracks);

          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref albumInfo.Compilation, albumMatch.Compilation);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref albumInfo.DiscNum, albumMatch.DiscNum);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref albumInfo.ReleaseDate, albumMatch.ReleaseDate);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref albumInfo.Sales, albumMatch.Sales);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref albumInfo.TotalDiscs, albumMatch.TotalDiscs);

          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateRatings(ref albumInfo.Rating, albumMatch.Rating);

          if(albumInfo.Genres.Count == 0)
            albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(albumInfo.Genres, albumMatch.Genres, true);
          albumInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(albumInfo.Awards, albumMatch.Awards, true);

          //These lists contain Ids and other properties that are not persisted, so they will always appear changed.
          //So changes to these lists will only be stored if something else has changed.
          MetadataUpdater.SetOrUpdateList(albumInfo.Artists, albumMatch.Artists, albumInfo.Artists.Count == 0);
          MetadataUpdater.SetOrUpdateList(albumInfo.MusicLabels, albumMatch.MusicLabels, albumInfo.MusicLabels.Count == 0);

          if (updateTrackList) //Comparing all tracks can be quite time consuming
          {
            MetadataUpdater.SetOrUpdateList(albumInfo.Tracks, albumMatch.Tracks, true);
            List<string> artists = new List<string>();
            foreach (TrackInfo track in albumMatch.Tracks)
            {
              if (track.Artists.Count > 0)
                if (!artists.Contains(track.Artists[0].Name))
                  artists.Add(track.Artists[0].Name);
            }
            if (albumMatch.Tracks.Count > 5 && (float)artists.Count > (float)albumMatch.Tracks.Count * 0.6 && !albumInfo.Compilation)
            {
              albumInfo.Compilation = true;
              albumInfo.HasChanged = true;
            }
          }

          if (albumInfo.Artists.Count > 0 && !albumInfo.Compilation &&
              albumInfo.Artists[0].Name.IndexOf("Various", StringComparison.InvariantCultureIgnoreCase) >= 0)
          {
            albumInfo.Compilation = true;
            albumInfo.HasChanged = true;
          }

          //Store person matches
          foreach (PersonInfo person in albumInfo.Artists)
          {
            if (GetPersonId(person, out id))
              _artistMatcher.StoreNameMatch(id, person.Name, person.Name);
          }

          //Store company matches
          foreach (CompanyInfo company in albumInfo.MusicLabels)
          {
            if (GetCompanyId(company, out id))
              _labelMatcher.StoreNameMatch(id, company.Name, company.Name);
          }
        }

        string Id;
        if (!GetTrackAlbumId(albumInfo, out Id))
        {
          //Store empty match so it is not retried
          if (!forceQuickMode)
            _albumMatcher.StoreNameMatch("", albumInfo.Album, albumInfo.Album);
        }

        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing collection {0}", ex, albumInfo.ToString());
        return false;
      }
    }

    public virtual bool FindAndUpdateTrackPerson(TrackInfo trackInfo, PersonInfo person, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(trackInfo.Languages);
        string id;
        bool updated = false;
        if (_artistMatcher.GetNameMatch(person.Name, out id))
        {
          if (SetPersonId(person, id))
            updated = true;
          else
            return false;
        }

        //Try updating from cache
        if (!_wrapper.UpdateFromOnlineMusicTrackPerson(trackInfo, person, language, true))
        {
          if (!forceQuickMode)
          {
            //Try to update person information from online source if online Ids are present
            if (!_wrapper.UpdateFromOnlineMusicTrackPerson(trackInfo, person, language, false))
            {
              //Search for the person online and update the Ids if a match is found
              if (_wrapper.SearchPersonUniqueAndUpdate(person, language))
              {
                //Ids were updated now try to fetch the online person info
                if (_wrapper.UpdateFromOnlineMusicTrackPerson(trackInfo, person, language, false))
                  updated = true;
              }
            }
            else
            {
              updated = true;
            }
          }
        }
        else
        {
          updated = true;
        }

        if (GetPersonId(person, out id))
        {
          _artistMatcher.StoreNameMatch(id, person.Name, person.Name);
        }
        else
        {
          //Store empty match so he/she is not retried
          if (!forceQuickMode)
            _artistMatcher.StoreNameMatch("", person.Name, person.Name);
        }

        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing person {0}", ex, person.ToString());
        return false;
      }
    }

    #endregion

    #region Metadata update helpers

    private string GetUniqueTrackName(TrackInfo trackInfo)
    {
      return string.Format("{0}: {1} - {2} [{3}]",
        !string.IsNullOrEmpty(trackInfo.Album) ? trackInfo.Album : "?",
        trackInfo.TrackNum > 0 ? trackInfo.TrackNum : 0,
        !string.IsNullOrEmpty(trackInfo.TrackName) ? trackInfo.TrackName : "?",
        trackInfo.Artists.Count > 0 ? trackInfo.Artists[0].Name : "?");
    }

    private void StoreTrackMatch(TrackInfo trackSearch, TrackInfo trackMatch)
    {
      string idValue = null;
      if (trackMatch == null || !GetTrackId(trackSearch, out idValue) || string.IsNullOrEmpty(trackMatch.TrackName))
      {
        //No match was found. Store search to avoid online search again
        _storage.TryAddMatch(new TrackMatch()
        {
          ItemName = GetUniqueTrackName(trackSearch),
        });
        return;
      }

      var onlineMatch = new TrackMatch
      {
        Id = idValue,
        ItemName = GetUniqueTrackName(trackSearch),
        ArtistName = trackMatch.Artists.Count > 0 ? trackMatch.Artists[0].Name : "",
        TrackName = !string.IsNullOrEmpty(trackMatch.TrackName) ? trackMatch.TrackName : "",
        AlbumName = !string.IsNullOrEmpty(trackMatch.Album) ? trackMatch.Album : "",
        TrackNum = trackMatch.TrackNum > 0 ? trackMatch.TrackNum : 0
      };
      _storage.TryAddMatch(onlineMatch);
    }

    protected virtual TLang FindBestMatchingLanguage(List<string> mediaLanguages)
    {
      if (typeof(TLang) == typeof(string))
      {
        CultureInfo mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture;
        // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
        if (mediaLanguages.Count == 0 || mediaLanguages.Contains(mpLocal.TwoLetterISOLanguageName))
          return (TLang)Convert.ChangeType(mpLocal.TwoLetterISOLanguageName, typeof(TLang));

        // If there is only one language available, use this one.
        if (mediaLanguages.Count == 1)
          return (TLang)Convert.ChangeType(mediaLanguages[0], typeof(TLang));
      }
      // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
      // By returning null we allow fallback to the default language of the online source (en).
      return default(TLang);
    }

    protected virtual TLang FindMatchingLanguage(string shortLanguageString)
    {
      if (typeof(TLang) == typeof(string) && !string.IsNullOrEmpty(shortLanguageString))
      {
        return (TLang)Convert.ChangeType(shortLanguageString, typeof(TLang));
      }
      return default(TLang);
    }

    #endregion

    #region Ids

    protected abstract bool GetTrackId(TrackInfo track, out string id);

    protected abstract bool SetTrackId(TrackInfo track, string id);

    protected virtual bool GetTrackAlbumId(AlbumInfo album, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool SetTrackAlbumId(AlbumInfo album, string id)
    {
      return false;
    }

    protected virtual bool GetPersonId(PersonInfo person, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool SetPersonId(PersonInfo person, string id)
    {
      return false;
    }

    protected virtual bool GetCompanyId(CompanyInfo company, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool SetCompanyId(CompanyInfo company, string id)
    {
      return false;
    }

    #endregion

    #region Caching

    /// <summary>
    /// Check if the memory cache should be cleared and starts an online update of (file-) cached series information.
    /// </summary>
    private void CheckCacheAndRefresh()
    {
      if (DateTime.Now - _memoryCacheInvalidated <= _maxCacheDuration)
        return;
      _memoryCache.Clear();
      _memoryCacheInvalidated = DateTime.Now;

      RefreshCache();
    }

    protected virtual void RefreshCache()
    {
      if (CacheRefreshable)
      {
        string dateFormat = "MMddyyyyHHmm";
        if (string.IsNullOrEmpty(_config.LastRefresh))
          _config.LastRefresh = DateTime.Now.ToString(dateFormat);

        DateTime lastRefresh = DateTime.ParseExact(_config.LastRefresh, dateFormat, CultureInfo.InvariantCulture);

        if (DateTime.Now - lastRefresh <= _maxCacheDuration)
          return;

        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>(false);
        if (threadPool != null)
        {
          Logger.Debug(_id + ": Refreshing local cache");
          threadPool.Add(() =>
          {
            if (_wrapper != null)
            {
              if(_wrapper.RefreshCache(lastRefresh))
                _config.LastRefresh = DateTime.Now.ToString(dateFormat, CultureInfo.InvariantCulture);
            }
          });
        }
      }
      SaveConfig();
    }

    #endregion

    #region FanArt

    public virtual bool ScheduleFanArtDownload(Guid mediaItemId, BaseInfo info)
    {
      string id;
      string mediaItem = mediaItemId.ToString().ToUpperInvariant();
      if (info is TrackInfo)
      {
        TrackInfo trackInfo = info as TrackInfo;
        if (GetTrackId(trackInfo, out id))
        {
          TLang language = FindBestMatchingLanguage(trackInfo.Languages);
          DownloadData data = new DownloadData()
          {
            FanArtMediaType = FanArtMediaTypes.Audio,
            ShortLanguage = language != null ? language.ToString() : "",
            MediaItemId = mediaItem,
            Name = trackInfo.ToString()
          };
          data.FanArtId[FanArtMediaTypes.Audio] = id;
          if (GetTrackAlbumId(trackInfo.CloneBasicInstance<AlbumInfo>(), out id))
          {
            data.FanArtId[FanArtMediaTypes.Album] = id;
          }
          return ScheduleDownload(id, data.Serialize());
        }
      }
      else if (info is AlbumInfo)
      {
        AlbumInfo albumInfo = info as AlbumInfo;
        if (GetTrackAlbumId(albumInfo, out id))
        {
          TLang language = FindBestMatchingLanguage(albumInfo.Languages);
          DownloadData data = new DownloadData()
          {
            FanArtMediaType = FanArtMediaTypes.Album,
            ShortLanguage = language != null ? language.ToString() : "",
            MediaItemId = mediaItem,
            Name = albumInfo.ToString()
          };
          data.FanArtId[FanArtMediaTypes.Album] = id;
          return ScheduleDownload(id, data.Serialize());
        }
      }
      else if (info is CompanyInfo)
      {
        CompanyInfo companyInfo = info as CompanyInfo;
        if (GetCompanyId(companyInfo, out id))
        {
          DownloadData data = new DownloadData()
          {
            FanArtMediaType = FanArtMediaTypes.MusicLabel,
            ShortLanguage = "",
            MediaItemId = mediaItem,
            Name = companyInfo.ToString()
          };
          data.FanArtId[FanArtMediaTypes.MusicLabel] = id;
          return ScheduleDownload(id, data.Serialize());
        }
      }
      else if (info is PersonInfo)
      {
        PersonInfo personInfo = info as PersonInfo;
        if (GetPersonId(personInfo, out id))
        {
          DownloadData data = new DownloadData()
          {
            ShortLanguage = "",
            MediaItemId = mediaItem,
            Name = personInfo.ToString()
          };
          if (personInfo.Occupation == PersonAspect.OCCUPATION_ARTIST)
          {
            data.FanArtMediaType = FanArtMediaTypes.Artist;
            data.FanArtId[FanArtMediaTypes.Artist] = id;
          }
          else if (personInfo.Occupation == PersonAspect.OCCUPATION_COMPOSER)
          {
            data.FanArtMediaType = FanArtMediaTypes.Writer;
            data.FanArtId[FanArtMediaTypes.Writer] = id;
          }
          return ScheduleDownload(id, data.Serialize());
        }
      }
      return false;
    }

    protected override void DownloadFanArt(string downloadId)
    {
      string name = downloadId;
      try
      {
        if (string.IsNullOrEmpty(downloadId))
          return;

        DownloadData data = new DownloadData();
        if (!data.Deserialize(downloadId))
          return;

        name = string.Format("{0} ({1})", data.MediaItemId, data.Name);

        if (!Init())
          return;

        string[] fanArtTypes = new string[]
        {
          FanArtTypes.FanArt,
          FanArtTypes.Poster,
          FanArtTypes.Banner,
          FanArtTypes.ClearArt,
          FanArtTypes.Cover,
          FanArtTypes.DiscArt,
          FanArtTypes.Logo,
          FanArtTypes.Thumbnail
        };

        try
        {
          TLang language = FindMatchingLanguage(data.ShortLanguage);
          foreach (string fanArtType in fanArtTypes)
            FanArtCache.InitFanArtCount(data.MediaItemId, fanArtType);

          Logger.Debug(_id + " Download: Started for media item {0}", name);
          ApiWrapperImageCollection<TImg> images = null;
          string Id = "";
          if (data.FanArtMediaType == FanArtMediaTypes.Audio)
          {
            Id = data.FanArtId[FanArtMediaTypes.Audio];
            TrackInfo trackInfo = new TrackInfo();
            if (SetTrackId(trackInfo, Id))
            {
              if (_wrapper.GetFanArt(trackInfo, language, data.FanArtMediaType, out images) == false)
              {
                Logger.Debug(_id + " Download: Failed getting images for track ID {0} [{1}]", Id, name);
                return;
              }
            }
          }
          else if (data.FanArtMediaType == FanArtMediaTypes.Album)
          {
            Id = data.FanArtId[FanArtMediaTypes.Album];
            AlbumInfo albumInfo = new AlbumInfo();
            if (SetTrackAlbumId(albumInfo, Id))
            {
              if (_wrapper.GetFanArt(albumInfo, language, data.FanArtMediaType, out images) == false)
              {
                Logger.Debug(_id + " Download: Failed getting images for album ID {0} [{1}]", Id, name);
                return;
              }
            }
          }
          else if (data.FanArtMediaType == FanArtMediaTypes.Artist || data.FanArtMediaType == FanArtMediaTypes.Writer)
          {
            if (OnlyBasicFanArt)
              return;

            Id = data.FanArtId[data.FanArtMediaType];
            PersonInfo personInfo = new PersonInfo();
            if (SetPersonId(personInfo, Id))
            {
              if (_wrapper.GetFanArt(personInfo, language, data.FanArtMediaType, out images) == false)
              {
                Logger.Debug(_id + " Download: Failed getting images for music person ID {0} [{1}]", Id, name);
                return;
              }
            }
          }
          else if (data.FanArtMediaType == FanArtMediaTypes.MusicLabel)
          {
            if (OnlyBasicFanArt)
              return;

            Id = data.FanArtId[FanArtMediaTypes.MusicLabel];
            CompanyInfo companyInfo = new CompanyInfo();
            if (SetCompanyId(companyInfo, Id))
            {
              if (_wrapper.GetFanArt(companyInfo, language, data.FanArtMediaType, out images) == false)
              {
                Logger.Debug(_id + " Download: Failed getting images for music company ID {0} [{1}]", Id, name);
                return;
              }
            }
          }
          if (images != null)
          {
            Logger.Debug(_id + " Download: Downloading images for ID {0} [{1}]", Id, name);

            SaveFanArtImages(images.Id, images.Backdrops, data.MediaItemId, data.Name, FanArtTypes.FanArt);
            SaveFanArtImages(images.Id, images.Posters, data.MediaItemId, data.Name, FanArtTypes.Poster);
            SaveFanArtImages(images.Id, images.Banners, data.MediaItemId, data.Name, FanArtTypes.Banner);
            SaveFanArtImages(images.Id, images.Covers, data.MediaItemId, data.Name, FanArtTypes.Cover);
            SaveFanArtImages(images.Id, images.Thumbnails, data.MediaItemId, data.Name, FanArtTypes.Thumbnail);

            if (!OnlyBasicFanArt)
            {
              SaveFanArtImages(images.Id, images.ClearArt, data.MediaItemId, data.Name, FanArtTypes.ClearArt);
              SaveFanArtImages(images.Id, images.DiscArt, data.MediaItemId, data.Name, FanArtTypes.DiscArt);
              SaveFanArtImages(images.Id, images.Logos, data.MediaItemId, data.Name, FanArtTypes.Logo);
            }

            Logger.Debug(_id + " Download: Finished saving images for ID {0} [{1}]", Id, name);
          }
        }
        finally
        {
          // Remember we are finished
          FinishDownloadFanArt(downloadId);
        }
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + " Download: Exception downloading images for {0}", ex, name);
      }
    }

    protected virtual bool VerifyFanArtImage(TImg image)
    {
      return image != null;
    }

    protected virtual int SaveFanArtImages(string id, IEnumerable<TImg> images, string mediaItemId, string name, string fanartType)
    {
      try
      {
        if (images == null)
          return 0;

        int idx = 0;
        foreach (TImg img in images)
        {
          using (FanArtCache.FanArtCountLock countLock = FanArtCache.GetFanArtCountLock(mediaItemId, fanartType))
          {
            if (countLock.Count >= FanArtCache.MAX_FANART_IMAGES[fanartType])
              break;
            if (!VerifyFanArtImage(img))
              continue;
            if (idx >= FanArtCache.MAX_FANART_IMAGES[fanartType])
              break;
            FanArtCache.InitFanArtCache(mediaItemId, name);
            if (_wrapper.DownloadFanArt(id, img, Path.Combine(FANART_CACHE_PATH, mediaItemId, fanartType)))
            {
              countLock.Count++;
              idx++;
            }
            else
            {
              Logger.Warn(_id + " Download: Error downloading FanArt for ID {0} on media item {1} ({2}) of type {3}", id, mediaItemId, name, fanartType);
            }
          }
        }
        Logger.Debug(_id + @" Download: Saved {0} for media item {1} ({2}) of type {3}", idx, mediaItemId, name, fanartType);
        return idx;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + " Download: Exception downloading images for ID {0} [{1} ({2})]", ex, id, mediaItemId, name);
        return 0;
      }
    }

    #endregion
  }
}
