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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public abstract class MusicMatcher<TImg, TLang> : BaseMatcher<TrackMatch, string, TImg, TLang>, IMusicMatcher
  {
    public class MusicMatcherSettings
    {
      public string LastRefresh { get; set; }

      public List<string> LastUpdatedAlbums { get; set; }

      public List<string> LastUpdatedTracks { get; set; }
    }
    
    protected readonly SemaphoreSlim _initSyncObj = new SemaphoreSlim(1, 1);
    protected bool _isInit = false;

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
      _conductorMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "ComposerMatches.xml"));
      _labelMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "LabelMatches.xml"));
      _albumMatcher = new SimpleNameMatcher(Path.Combine(cachePath, "AlbumMatches.xml"));
      _configFile = Path.Combine(cachePath, "MusicConfig.xml");
    }

    public override async Task<bool> InitAsync()
    {
      if (!_enabled)
        return false;

      await _initSyncObj.WaitAsync().ConfigureAwait(false);
      try
      {
        if (_isInit)
          return true;

        if (!await base.InitAsync().ConfigureAwait(false))
          return false;

        LoadConfig();

        if (await InitWrapperAsync(UseSecureWebCommunication).ConfigureAwait(false))
        {
          if (_wrapper != null)
            _wrapper.CacheUpdateFinished += CacheUpdateFinished;
          _isInit = true;
          return true;
        }
        return false;
      }
      finally
      {
        _initSyncObj.Release();
      }
    }

    private void LoadConfig()
    {
      _config = Settings.Load<MusicMatcherSettings>(_configFile);
      if (_config == null)
        _config = new MusicMatcherSettings();
      if (_config.LastRefresh != null)
        _lastCacheRefresh = DateTime.ParseExact(_config.LastRefresh, CONFIG_DATE_FORMAT, CultureInfo.InvariantCulture);
      if (_config.LastUpdatedAlbums == null)
        _config.LastUpdatedAlbums = new List<string>();
      if (_config.LastUpdatedTracks == null)
        _config.LastUpdatedTracks = new List<string>();
    }

    private void SaveConfig()
    {
      Settings.Save(_configFile, _config);
    }

    public abstract Task<bool> InitWrapperAsync(bool useHttps);

    #endregion

    #region Constants
    
    private TimeSpan CACHE_CHECK_INTERVAL = TimeSpan.FromMinutes(60);

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
    private bool _cacheRefreshable;
    private DateTime? _lastCacheRefresh;
    private DateTime _lastCacheCheck = DateTime.MinValue;
    private string _preferredLanguageCulture = "en-US";

    private SimpleNameMatcher _artistMatcher;
    private SimpleNameMatcher _composerMatcher;
    private SimpleNameMatcher _conductorMatcher;
    private SimpleNameMatcher _labelMatcher;
    private SimpleNameMatcher _albumMatcher;

    #endregion

    #region Properties

    public bool Enabled
    {
      get { return _enabled; }
      set { _enabled = value; }
    }

    public bool CacheRefreshable
    {
      get { return _cacheRefreshable; }
    }

    public string PreferredLanguageCulture
    {
      get { return _preferredLanguageCulture; }
      set { _preferredLanguageCulture = value; }
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

    public virtual void StoreConductorMatch(PersonInfo person)
    {
      string id;
      if (GetPersonId(person, out id))
        _conductorMatcher.StoreNameMatch(id, person.Name, person.Name);
    }

    public virtual void StoreMusicLabelMatch(CompanyInfo company)
    {
      string id;
      if (GetCompanyId(company, out id))
        _labelMatcher.StoreNameMatch(id, company.Name, company.Name);
    }

    public virtual void StoreAlbumMatch(AlbumInfo albumSearch, AlbumInfo albumMatch)
    {
      string id = "";
      if (!GetTrackAlbumId(albumSearch, out id))
        id = "";
      _albumMatcher.StoreNameMatch(id, GetUniqueAlbumName(albumSearch), GetUniqueAlbumName(albumMatch));
    }

    #endregion

    #region Metadata updaters

    /// <summary>
    /// Tries to lookup the music track online and downloads images.
    /// </summary>
    /// <param name="trackInfo">Track to check</param>
    /// <returns><c>true</c> if successful</returns>
    public virtual async Task<bool> FindAndUpdateTrackAsync(TrackInfo trackInfo)
    {
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
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
            ((trackInfo.Artists.Count > 0 && !string.IsNullOrEmpty(m.ArtistName) ? trackInfo.Artists[0].Name.Equals(m.ArtistName, StringComparison.OrdinalIgnoreCase) : false) || trackInfo.Artists.Count == 0) &&
            ((!string.IsNullOrEmpty(trackInfo.Album) && !string.IsNullOrEmpty(m.AlbumName) ? trackInfo.Album.Equals(m.AlbumName, StringComparison.OrdinalIgnoreCase) : false) || string.IsNullOrEmpty(trackInfo.Album)) &&
            ((trackInfo.TrackNum > 0 && m.TrackNum > 0 && int.Equals(m.TrackNum, trackInfo.TrackNum)) || trackInfo.TrackNum <= 0));
          Logger.Debug(_id + ": Try to lookup track \"{0}\" from cache: {1}", trackInfo, match != null && !string.IsNullOrEmpty(match.Id));

          trackMatch = trackInfo.Clone();
          if (match != null)
          {
            if (SetTrackId(trackMatch, match.Id))
            {
              //If Id was found in cache the online track info is probably also in the cache
              if (await _wrapper.UpdateFromOnlineMusicTrackAsync(trackMatch, language, true).ConfigureAwait(false))
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

          if (!matchFound)
          {
            Logger.Debug(_id + ": Search for track {0} online", trackInfo.ToString());

            //Try to update track information from online source if online Ids are present
            if (!await _wrapper.UpdateFromOnlineMusicTrackAsync(trackMatch, language, false).ConfigureAwait(false))
            {
              //Search for the track online and update the Ids if a match is found
              if (await _wrapper.SearchTrackUniqueAndUpdateAsync(trackMatch, language).ConfigureAwait(false))
              {
                //Ids were updated now try to update track information from online source
                if (await _wrapper.UpdateFromOnlineMusicTrackAsync(trackMatch, language, false).ConfigureAwait(false))
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
        StoreTrackMatch(trackInfo, trackMatch);

        if (matchFound)
        {
            //MP2-593: Don't update track/disc number properties from the web. If some tracks of the album don't get
            //matched and the disc numbers of the others have been updated the ordering of tracks becomes incorrect
            //as the unmatched tracks may be set to disc 0, whilst the matched set to disc 1
          trackInfo.MergeWith(trackMatch, false, false);

          if (trackInfo.Genres.Count > 0)
          {
            IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
            foreach (var genre in trackInfo.Genres)
            {
              if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Music, null, out int genreId))
              {
                genre.Id = genreId;
                trackInfo.HasChanged = true;
              }
            }
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
          foreach (PersonInfo person in trackInfo.Conductors)
          {
            string id;
            if (GetPersonId(person, out id))
              _conductorMatcher.StoreNameMatch(id, person.Name, person.Name);
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

    public virtual async Task<bool> UpdateTrackPersonsAsync(TrackInfo trackInfo, string occupation, bool forAlbum)
    {
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return false;

        TLang language = FindBestMatchingLanguage(trackInfo.Languages);
        bool updated = false;
        TrackInfo trackMatch = trackInfo.Clone();
        List<PersonInfo> persons = new List<PersonInfo>();
        if (occupation == PersonAspect.OCCUPATION_ARTIST && forAlbum)
        {
          foreach (PersonInfo person in trackMatch.AlbumArtists)
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
        else if (occupation == PersonAspect.OCCUPATION_ARTIST)
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
        else if (occupation == PersonAspect.OCCUPATION_CONDUCTOR)
        {
          foreach (PersonInfo person in trackMatch.Conductors)
          {
            string id;
            if (_conductorMatcher.GetNameMatch(person.Name, out id))
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

        if (persons.Count == 0)
          return true;

        foreach (PersonInfo person in persons)
        {
          //Try updating from cache
          if (!await _wrapper.UpdateFromOnlineMusicTrackPersonAsync(trackMatch, person, language, true).ConfigureAwait(false))
          {
            Logger.Debug(_id + ": Search for person {0} online", person.ToString());

            //Try to update person information from online source if online Ids are present
            if (!await _wrapper.UpdateFromOnlineMusicTrackPersonAsync(trackMatch, person, language, false).ConfigureAwait(false))
            {
              //Search for the person online and update the Ids if a match is found
              if (await _wrapper.SearchPersonUniqueAndUpdateAsync(person, language).ConfigureAwait(false))
              {
                //Ids were updated now try to fetch the online person info
                if (await _wrapper.UpdateFromOnlineMusicTrackPersonAsync(trackMatch, person, language, false).ConfigureAwait(false))
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
          if (forAlbum)
            album.Artists = trackMatch.AlbumArtists;
          else
            album.Artists = trackMatch.Artists;
          if (await UpdateAlbumPersonsAsync(album, occupation).ConfigureAwait(false))
          {
            trackMatch.HasChanged = album.HasChanged ? album.HasChanged : trackMatch.HasChanged;
            updated = true;
          }
        }

        if (updated)
        {
          //These lists contain Ids and other properties that are not loaded, so they will always appear changed.
          //So these changes will be ignored and only stored if there is any other reason for it to have changed.
          if (occupation == PersonAspect.OCCUPATION_ARTIST && forAlbum)
            MetadataUpdater.SetOrUpdateList(trackInfo.AlbumArtists, trackMatch.AlbumArtists.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), false, false);
          else if (occupation == PersonAspect.OCCUPATION_ARTIST)
            MetadataUpdater.SetOrUpdateList(trackInfo.Artists, trackMatch.Artists.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), false, false);
          else if (occupation == PersonAspect.OCCUPATION_COMPOSER)
            MetadataUpdater.SetOrUpdateList(trackInfo.Composers, trackMatch.Composers.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), false, false);
          else if (occupation == PersonAspect.OCCUPATION_CONDUCTOR)
            MetadataUpdater.SetOrUpdateList(trackInfo.Conductors, trackMatch.Conductors.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), false, false);
        }

        List<string> thumbs = new List<string>();
        if (occupation == PersonAspect.OCCUPATION_ARTIST && forAlbum)
        {
          foreach (PersonInfo person in trackInfo.AlbumArtists)
          {
            string id;
            if (GetPersonId(person, out id))
            {
              _artistMatcher.StoreNameMatch(id, person.Name, person.Name);
            }
            else
            {
              //Store empty match so he/she is not retried
              _artistMatcher.StoreNameMatch("", person.Name, person.Name);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_ARTIST)
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
        else if (occupation == PersonAspect.OCCUPATION_CONDUCTOR)
        {
          foreach (PersonInfo person in trackInfo.Conductors)
          {
            string id;
            if (GetPersonId(person, out id))
            {
              _conductorMatcher.StoreNameMatch(id, person.Name, person.Name);
            }
            else
            {
              //Store empty match so he/she is not retried
              _conductorMatcher.StoreNameMatch("", person.Name, person.Name);
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

    public virtual async Task<bool> UpdateAlbumPersonsAsync(AlbumInfo albumInfo, string occupation)
    {
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
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

        if (persons.Count == 0)
          return true;

        foreach (PersonInfo person in persons)
        {
          //Try updating from cache
          if (!await _wrapper.UpdateFromOnlineMusicTrackAlbumPersonAsync(albumMatch, person, language, true).ConfigureAwait(false))
          {
            Logger.Debug(_id + ": Search for person {0} online", person.ToString());

            //Try to update person information from online source if online Ids are present
            if (!await _wrapper.UpdateFromOnlineMusicTrackAlbumPersonAsync(albumMatch, person, language, false).ConfigureAwait(false))
            {
              //Search for the person online and update the Ids if a match is found
              if (await _wrapper.SearchPersonUniqueAndUpdateAsync(person, language).ConfigureAwait(false))
              {
                //Ids were updated now try to fetch the online person info
                if (await _wrapper.UpdateFromOnlineMusicTrackAlbumPersonAsync(albumMatch, person, language, false).ConfigureAwait(false))
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
            MetadataUpdater.SetOrUpdateList(albumInfo.Artists, albumMatch.Artists.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), false, false);
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

    public virtual async Task<bool> UpdateAlbumCompaniesAsync(AlbumInfo albumInfo, string companyType)
    {
      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
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

        if (companies.Count == 0)
          return true;

        foreach (CompanyInfo company in companies)
        {
          //Try updating from cache
          if (!await _wrapper.UpdateFromOnlineMusicTrackAlbumCompanyAsync(albumMatch, company, language, true).ConfigureAwait(false))
          {
            Logger.Debug(_id + ": Search for company {0} online", company.ToString());

            //Try to update company information from online source if online Ids are present
            if (!await _wrapper.UpdateFromOnlineMusicTrackAlbumCompanyAsync(albumMatch, company, language, false).ConfigureAwait(false))
            {
              //Search for the company online and update the Ids if a match is found
              if (await _wrapper.SearchCompanyUniqueAndUpdateAsync(company, language).ConfigureAwait(false))
              {
                //Ids were updated now try to fetch the online company info
                if (await _wrapper.UpdateFromOnlineMusicTrackAlbumCompanyAsync(albumMatch, company, language, false).ConfigureAwait(false))
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
            MetadataUpdater.SetOrUpdateList(albumInfo.MusicLabels, albumMatch.MusicLabels.Where(c => !string.IsNullOrEmpty(c.Name)).Distinct().ToList(), false);
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

    public virtual async Task<bool> UpdateAlbumAsync(AlbumInfo albumInfo, bool updateTrackList)
    {
      try
      {
        if (string.IsNullOrEmpty(albumInfo.Album))
          return false;

        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return false;

        TLang language = FindBestMatchingLanguage(albumInfo.Languages);
        string id;
        if (!GetTrackAlbumId(albumInfo, out id))
        {
          if (_albumMatcher.GetNameMatch(GetUniqueAlbumName(albumInfo), out id))
          {
            if (!SetTrackAlbumId(albumInfo, id))
            {
              //Match probably stored with invalid Id to avoid retries. 
              //Searching for this album by name only failed so stop trying.
              return false;
            }
          }
        }

        bool updated = false;
        AlbumInfo albumMatch = albumInfo.Clone();
        albumMatch.Tracks.Clear();
        //Try updating from cache
        if (!await _wrapper.UpdateFromOnlineMusicTrackAlbumAsync(albumMatch, language, true).ConfigureAwait(false))
        {
          Logger.Debug(_id + ": Search for album {0} online", albumInfo.ToString());

          //Try to update company information from online source if online Ids are present
          if (!await _wrapper.UpdateFromOnlineMusicTrackAlbumAsync(albumMatch, language, false).ConfigureAwait(false))
          {
            //Search for the company online and update the Ids if a match is found
            if (await _wrapper.SearchTrackAlbumUniqueAndUpdateAsync(albumMatch, language).ConfigureAwait(false))
            {
              //Ids were updated now try to fetch the online company info
              if (await _wrapper.UpdateFromOnlineMusicTrackAlbumAsync(albumMatch, language, false).ConfigureAwait(false))
                updated = true;
            }
          }
          else
          {
            updated = true;
          }
        }
        else
        {
          Logger.Debug(_id + ": Found album {0} in cache", albumInfo.ToString());
          updated = true;
        }

        if (updated)
        {
          albumInfo.MergeWith(albumMatch, false, updateTrackList);

          IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
          if (albumInfo.Genres.Count > 0)
          {
            foreach (var genre in albumInfo.Genres)
            {
              if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Music, language.ToString(), out int genreId))
              {
                genre.Id = genreId;
                albumInfo.HasChanged = true;
              }
            }
          }

          if (updateTrackList)
          {
            foreach (TrackInfo track in albumMatch.Tracks)
            {
              foreach (var genre in track.Genres)
              {
                if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Music, language.ToString(), out int genreId))
                {
                  genre.Id = genreId;
                  albumInfo.HasChanged = true;
                }
              }
            }
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

        StoreAlbumMatch(albumInfo, albumMatch);
        return updated;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while processing album {0}", ex, albumInfo.ToString());
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

    private string GetUniqueAlbumName(AlbumInfo albumInfo)
    {
      return string.Format("{0}: {1} - {2}",
        !string.IsNullOrEmpty(albumInfo.Album) ? albumInfo.Album : "?",
        albumInfo.Artists.Count > 0 ? albumInfo.Artists[0].Name : "?",
        albumInfo.ReleaseDate.HasValue ? albumInfo.ReleaseDate.Value.Year.ToString() : "?");
    }

    private void StoreTrackMatch(TrackInfo trackSearch, TrackInfo trackMatch)
    {
      string idValue = null;
      if (trackMatch == null || !GetTrackId(trackMatch, out idValue) || string.IsNullOrEmpty(trackMatch.TrackName))
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
        CultureInfo mpLocal = new CultureInfo(_preferredLanguageCulture);
        // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
        if (mediaLanguages.Count == 0 || mediaLanguages.Contains(mpLocal.TwoLetterISOLanguageName))
          return (TLang)Convert.ChangeType(mpLocal.TwoLetterISOLanguageName, typeof(TLang));

        // If there is only one language available, use this one.
        if (mediaLanguages.Count == 1)
          return (TLang)Convert.ChangeType(mediaLanguages[0], typeof(TLang));

        // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
        // Use the preferred language.
        return (TLang)Convert.ChangeType(mpLocal.TwoLetterISOLanguageName, typeof(TLang));
      }
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
      if (CacheRefreshable && Enabled)
      {
        string dateFormat = "MMddyyyyHHmm";
        if (!_lastCacheRefresh.HasValue)
        {
          if (string.IsNullOrEmpty(_config.LastRefresh))
            _config.LastRefresh = DateTime.Now.ToString(dateFormat);

          _lastCacheRefresh = DateTime.ParseExact(_config.LastRefresh, dateFormat, CultureInfo.InvariantCulture);
        }

        if (DateTime.Now - _lastCacheCheck <= CACHE_CHECK_INTERVAL)
          return;

        _lastCacheCheck = DateTime.Now;

        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>(false);
        if (threadPool != null)
        {
          Logger.Debug(_id + ": Checking local cache");
          threadPool.Add(() =>
          {
            if (_wrapper != null)
            {
              if (_wrapper.RefreshCache(_lastCacheRefresh.Value))
              {
                _lastCacheRefresh = DateTime.Now;
                _config.LastRefresh = _lastCacheRefresh.Value.ToString(dateFormat, CultureInfo.InvariantCulture);
              }
            }
          });
        }
        SaveConfig();
      }
    }

    private void CacheUpdateFinished(ApiWrapper<TImg, TLang>.UpdateFinishedEventArgs _event)
    {
      try
      {
        if (_event.UpdatedItemType == ApiWrapper<TImg, TLang>.UpdateType.AudioAlbum)
        {
          _config.LastUpdatedAlbums.AddRange(_event.UpdatedItems);
          SaveConfig();
        }
        if (_event.UpdatedItemType == ApiWrapper<TImg, TLang>.UpdateType.Audio)
        {
          _config.LastUpdatedTracks.AddRange(_event.UpdatedItems);
          SaveConfig();
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex);
      }
    }

    public List<AlbumInfo> GetLastChangedAudioAlbums()
    {
      List<AlbumInfo> albums = new List<AlbumInfo>();

      if (!InitAsync().Result)
        return albums;

      foreach (string id in _config.LastUpdatedAlbums)
      {
        AlbumInfo a = new AlbumInfo();
        if (SetTrackAlbumId(a, id) && !albums.Contains(a))
          albums.Add(a);
      }
      return albums;
    }

    public void ResetLastChangedAudioAlbums()
    {
      if (!InitAsync().Result)
        return;

      _config.LastUpdatedAlbums.Clear();
      SaveConfig();
    }

    public List<TrackInfo> GetLastChangedAudio()
    {
      List<TrackInfo> tracks = new List<TrackInfo>();
      if (!InitAsync().Result)
        return tracks;

      foreach (string id in _config.LastUpdatedTracks)
      {
        TrackInfo t = new TrackInfo();
        if (SetTrackId(t, id) && !tracks.Contains(t))
          tracks.Add(t);
      }
      return tracks;
    }

    public void ResetLastChangedAudio()
    {
      if (!InitAsync().Result)
        return;

      _config.LastUpdatedTracks.Clear();
      SaveConfig();
    }

    #endregion

    #region FanArt

    protected override bool TryGetFanArtInfo(BaseInfo info, out TLang language, out string fanArtMediaType, out bool includeThumbnails)
    {
      language = default(TLang);
      fanArtMediaType = null;
      includeThumbnails = true;

      TrackInfo trackInfo = info as TrackInfo;
      if (trackInfo != null)
      {
        language = FindBestMatchingLanguage(trackInfo.Languages);
        fanArtMediaType = FanArtMediaTypes.Audio;
        return true;
      }

      AlbumInfo albumInfo = info as AlbumInfo;
      if (albumInfo != null)
      {
        language = FindBestMatchingLanguage(albumInfo.Languages);
        fanArtMediaType = FanArtMediaTypes.Album;
        return true;
      }

      if (OnlyBasicFanArt)
        return false;

      CompanyInfo companyInfo = info as CompanyInfo;
      if (companyInfo != null)
      {
        language = FindMatchingLanguage(string.Empty);
        fanArtMediaType = FanArtMediaTypes.MusicLabel;
        return true;
      }

      PersonInfo personInfo = info as PersonInfo;
      if (personInfo != null)
      {
        if (personInfo.Occupation == PersonAspect.OCCUPATION_ARTIST)
          fanArtMediaType = FanArtMediaTypes.Artist;
        else if (personInfo.Occupation == PersonAspect.OCCUPATION_COMPOSER)
          fanArtMediaType = FanArtMediaTypes.Writer;
        else
          return false;
        language = FindMatchingLanguage(string.Empty);
        return true;
      }
      return false;
    }

    #endregion
  }
}
