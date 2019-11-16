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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Libraries;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="OnlineMatcherService"/> searches for metadata from online sources.
  /// </summary>
  public class OnlineMatcherService : IOnlineMatcherService, IDisposable
  {
    private List<IAudioMatcher> _audioMatchers = new List<IAudioMatcher>();
    private List<ISeriesMatcher> _seriesMatchers = new List<ISeriesMatcher>();
    private List<IMovieMatcher> _movieMatchers = new List<IMovieMatcher>();
    private List<ISubtitleMatcher> _subtitleMatchers = new List<ISubtitleMatcher>();
    private SettingsChangeWatcher<OnlineLibrarySettings> _settingChangeWatcher = null;

    protected IPluginItemStateTracker _onlineProviderPluginItemStateTracker;
    protected bool _providersInited = false;
    protected readonly object _syncObj = new object();

    #region Static instance

    public static IOnlineMatcherService Instance
    {
      get { return ServiceRegistration.Get<IOnlineMatcherService>(); }
    }

    #endregion

    public OnlineMatcherService()
    {
      _audioMatchers.Add(new MusicTheAudioDbMatcher());
      _audioMatchers.Add(new MusicFreeDbMatcher());
      _audioMatchers.Add(new MusicBrainzMatcher());
      _audioMatchers.Add(new MusicFanArtTvMatcher());

      _movieMatchers.Add(new MovieTheMovieDbMatcher());
      //_movieMatchers.Add(new MovieOmDbMatcher());
      _movieMatchers.Add(new MovieSimApiMatcher());
      _movieMatchers.Add(new MovieFanArtTvMatcher());

      _seriesMatchers.Add(new SeriesTvDbMatcher());
      _seriesMatchers.Add(new SeriesTheMovieDbMatcher());
      _seriesMatchers.Add(new SeriesTvMazeMatcher());
      //_seriesMatchers.Add(new SeriesOmDbMatcher());
      _seriesMatchers.Add(new SeriesFanArtTvMatcher());

      _subtitleMatchers.Add(new SubDbMatcher());
      _subtitleMatchers.Add(new SubsMaxMatcher());

      InitProviders();

      //Load settings
      LoadSettings();

      //Save settings
      SaveSettings();

      _settingChangeWatcher = new SettingsChangeWatcher<OnlineLibrarySettings>();
      _settingChangeWatcher.SettingsChanged += SettingsChanged;
    }

    private void LoadSettings()
    {
      OnlineLibrarySettings settings = ServiceRegistration.Get<ISettingsManager>().Load<OnlineLibrarySettings>();

      //Music matchers
      ConfigureMatchers(_audioMatchers, settings.MusicMatchers, settings.MusicLanguageCulture, settings.UseMusicAudioLanguageIfUnmatched);

      //Movie matchers
      ConfigureMatchers(_movieMatchers, settings.MovieMatchers, settings.MovieLanguageCulture, settings.UseMovieAudioLanguageIfUnmatched);

      //Series matchers
      ConfigureMatchers(_seriesMatchers, settings.SeriesMatchers, settings.SeriesLanguageCulture, settings.UseSeriesAudioLanguageIfUnmatched);

      //Subtitle matchers
      ConfigureMatchers(_subtitleMatchers, settings.SubtitleMatchers, settings.SubtitleLanguageCulture, false);
    }

    protected void ConfigureMatchers<T>(ICollection<T> matchers, ICollection<MatcherSetting> settings, string languageCulture, bool useMediaAudioIfUnmatched) where T : IMatcher
    {
      foreach (MatcherSetting setting in settings)
      {
        IMatcher matcher = matchers.FirstOrDefault(m => m.Id.Equals(setting.Id, StringComparison.OrdinalIgnoreCase));
        if (matcher != null)
        {
          matcher.Enabled = setting.Enabled;
          matcher.PreferredLanguageCulture = languageCulture;
          matcher.UseMediaAudioIfUnmatched = useMediaAudioIfUnmatched;
        }
      }
    }

    private void SaveSettings()
    {
      OnlineLibrarySettings settings = ServiceRegistration.Get<ISettingsManager>().Load<OnlineLibrarySettings>();
      List<MatcherSetting> list = new List<MatcherSetting>();
      foreach (IAudioMatcher matcher in _audioMatchers)
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Name = matcher.Name;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.MusicMatchers = list.ToArray();

      list.Clear();
      foreach (IMovieMatcher matcher in _movieMatchers)
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Name = matcher.Name;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.MovieMatchers = list.ToArray();

      list.Clear();
      foreach (ISeriesMatcher matcher in _seriesMatchers)
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Name = matcher.Name;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.SeriesMatchers = list.ToArray();

      list.Clear();
      foreach (ISubtitleMatcher matcher in _subtitleMatchers)
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Name = matcher.Name;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.SubtitleMatchers = list.ToArray();

      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    private async Task<IList<T>> MergeResults<T>(List<Task<IEnumerable<T>>> results) where T: BaseInfo
    {
      await Task.WhenAny(Task.WhenAll(results), Task.Delay(10000)).ConfigureAwait(false);

      List<T> list = new List<T>();
      foreach (var task in results)
      {
        if (!task.IsCompleted)
          continue;
        if (list.Count == 0)
        {
          list.AddRange(task.Result);
        }
        else if (task.Result != null)
        {
          foreach (var item in task.Result)
          {
            int idx = list.IndexOf(item);
            if (idx >= 0)
              list[idx].MergeWith(item);
            else
              list.Add(item);
          }
        }
      }
      return list;
    }


    #region Providers

    private IEnumerable<T> InitProviders<T>(string providerPath)
    {
      List<T> providers = new List<T>();
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(providerPath))
      {
        try
        {
          OnlineProviderRegistration onlineProviderRegistration = pluginManager.RequestPluginItem<OnlineProviderRegistration>(providerPath, itemMetadata.Id, _onlineProviderPluginItemStateTracker);
          if (onlineProviderRegistration == null || onlineProviderRegistration.ProviderClass == null)
            ServiceRegistration.Get<ILogger>().Warn("Could not instantiate {1} with id '{0}'", itemMetadata.Id, typeof(T).Name);
          else
          {
            object provider = Activator.CreateInstance(onlineProviderRegistration.ProviderClass);
            if (!(provider is T))
              throw new PluginInvalidStateException("Could not create {1} instance of class {0}", onlineProviderRegistration.ProviderClass, typeof(T).Name);
            providers.Add((T)provider);
          }
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot add {1} with id '{0}'", e, itemMetadata.Id, typeof(T).Name);
        }
      }
      return providers;
    }

    private void InitProviders()
    {
      lock (_syncObj)
      {
        if (_providersInited)
          return;

        _providersInited = true;
        _onlineProviderPluginItemStateTracker = new FixedItemStateTracker("OnlineMatcher Service - Provider registration");

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();

        //Audio providers
        _audioMatchers.AddRange(InitProviders<IAudioMatcher>(OnlineProviderBuilder.AUDIO_PROVIDER_PATH));

        //Movie providers
        _movieMatchers.AddRange(InitProviders<IMovieMatcher>(OnlineProviderBuilder.MOVIE_PROVIDER_PATH));

        //Series providers
        _seriesMatchers.AddRange(InitProviders<ISeriesMatcher>(OnlineProviderBuilder.SERIES_PROVIDER_PATH));

        //Subtitle providers
        _subtitleMatchers.AddRange(InitProviders<ISubtitleMatcher>(OnlineProviderBuilder.SUBTITLE_PROVIDER_PATH));
      }
    }

    public void Dispose()
    {
      foreach (IDisposable result in _audioMatchers.OfType<IDisposable>())
        result.Dispose();
      foreach (IDisposable result in _movieMatchers.OfType<IDisposable>())
        result.Dispose();
      foreach (IDisposable result in _seriesMatchers.OfType<IDisposable>())
        result.Dispose();
      foreach (IDisposable result in _subtitleMatchers.OfType<IDisposable>())
        result.Dispose();
    }

    #endregion

    #region Audio

    public List<AlbumInfo> GetLastChangedAudioAlbums()
    {
      List<AlbumInfo> albums = new List<AlbumInfo>();
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        foreach (AlbumInfo album in matcher.GetLastChangedAudioAlbums())
          if (!albums.Contains(album))
            albums.Add(album);
      }
      return albums;
    }

    public void ResetLastChangedAudioAlbums()
    {
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedAudioAlbums();
      }
    }

    public List<TrackInfo> GetLastChangedAudio()
    {
      List<TrackInfo> tracks = new List<TrackInfo>();
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        foreach (TrackInfo track in matcher.GetLastChangedAudio())
          if (!tracks.Contains(track))
            tracks.Add(track);
      }
      return tracks;
    }

    public void ResetLastChangedAudio()
    {
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedAudio();
      }
    }

    public async Task<IEnumerable<TrackInfo>> FindMatchingTracksAsync(TrackInfo trackInfo)
    {
      var tasks = _audioMatchers.Where(m => m.Enabled)
        .Select(m => m.FindMatchingTracksAsync(trackInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<IEnumerable<AlbumInfo>> FindMatchingAlbumsAsync(AlbumInfo albumInfo)
    {
      var tasks = _audioMatchers.Where(m => m.Enabled)
        .Select(m => m.FindMatchingAlbumsAsync(albumInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> FindAndUpdateTrackAsync(TrackInfo trackInfo)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.FindAndUpdateTrackAsync(trackInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateAlbumPersonsAsync(AlbumInfo albumInfo, string occupation)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateAlbumPersonsAsync(albumInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateTrackPersonsAsync(TrackInfo trackInfo, string occupation, bool forAlbum)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateTrackPersonsAsync(trackInfo, occupation, forAlbum).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateAlbumCompaniesAsync(AlbumInfo albumInfo, string companyType)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateAlbumCompaniesAsync(albumInfo, companyType).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateAlbumAsync(AlbumInfo albumInfo, bool updateTrackList)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateAlbumAsync(albumInfo, updateTrackList).ConfigureAwait(false);
      }

      if (updateTrackList)
      {
        if (albumInfo.Tracks.Count == 0)
          return false;

        for (int i = 0; i < albumInfo.Tracks.Count; i++)
        {
          //TrackInfo trackInfo = albumInfo.Tracks[i];
          //foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
          //{
          //  matcher.FindAndUpdateTrack(trackInfo, importOnly);
          //}
        }
      }
      return success;
    }

    public async Task<bool> DownloadAudioFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in _audioMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.DownloadFanArtAsync(mediaItemId, mediaItemInfo).ConfigureAwait(false);
      }
      return success;
    }

    public void StoreAudioPersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ARTIST)
      {
        foreach (IAudioMatcher matcher in _audioMatchers)
        {
          matcher.StoreArtistMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_COMPOSER)
      {
        foreach (IAudioMatcher matcher in _audioMatchers)
        {
          matcher.StoreComposerMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_CONDUCTOR)
      {
        foreach (IAudioMatcher matcher in _audioMatchers)
        {
          matcher.StoreConductorMatch(person);
        }
      }
    }

    public void StoreAudioCompanyMatch(CompanyInfo company)
    {
      if (company.Type == CompanyAspect.COMPANY_MUSIC_LABEL)
      {
        foreach (IAudioMatcher matcher in _audioMatchers)
        {
          matcher.StoreMusicLabelMatch(company);
        }
      }
    }

    #endregion

    #region Movie

    public List<MovieInfo> GetLastChangedMovies()
    {
      List<MovieInfo> movies = new List<MovieInfo>();
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        foreach (MovieInfo movie in matcher.GetLastChangedMovies())
          if (!movies.Contains(movie))
            movies.Add(movie);
      }
      return movies;
    }

    public void ResetLastChangedMovies()
    {
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedMovies();
      }
    }

    public List<MovieCollectionInfo> GetLastChangedMovieCollections()
    {
      List<MovieCollectionInfo> collections = new List<MovieCollectionInfo>();
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        foreach (MovieCollectionInfo collection in matcher.GetLastChangedMovieCollections())
          if (!collections.Contains(collection))
            collections.Add(collection);
      }
      return collections;
    }

    public void ResetLastChangedMovieCollections()
    {
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedMovieCollections();
      }
    }

    public async Task<IEnumerable<MovieInfo>> FindMatchingMoviesAsync(MovieInfo movieInfo)
    {
      var tasks = _movieMatchers.Where(m => m.Enabled)
        .Select(m => m.FindMatchingMoviesAsync(movieInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> FindAndUpdateMovieAsync(MovieInfo movieInfo)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.FindAndUpdateMovieAsync(movieInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdatePersonsAsync(MovieInfo movieInfo, string occupation)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdatePersonsAsync(movieInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateCharactersAsync(MovieInfo movieInfo)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateCharactersAsync(movieInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateCollectionAsync(MovieCollectionInfo collectionInfo, bool updateMovieList)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateCollectionAsync(collectionInfo, updateMovieList).ConfigureAwait(false);
      }

      if (updateMovieList)
      {
        if (collectionInfo.Movies.Count == 0)
          return false;

        for (int i = 0; i < collectionInfo.Movies.Count; i++)
        {
          //MovieInfo movieInfo = collectionInfo.Movies[i];
          //foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
          //{
          //  success |= matcher.FindAndUpdateMovie(movieInfo, importOnly);
          //}
        }
      }
      return success;
    }

    public async Task<bool> UpdateCompaniesAsync(MovieInfo movieInfo, string companyType)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateCompaniesAsync(movieInfo, companyType).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> DownloadMovieFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in _movieMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.DownloadFanArtAsync(mediaItemId, mediaItemInfo).ConfigureAwait(false);
      }
      return success;
    }

    public void StoreMoviePersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ACTOR)
      {
        foreach (IMovieMatcher matcher in _movieMatchers)
        {
          matcher.StoreActorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
      {
        foreach (IMovieMatcher matcher in _movieMatchers)
        {
          matcher.StoreDirectorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_WRITER)
      {
        foreach (IMovieMatcher matcher in _movieMatchers)
        {
          matcher.StoreWriterMatch(person);
        }
      }
    }

    public void StoreMovieCharacterMatch(CharacterInfo character)
    {
      foreach (IMovieMatcher matcher in _movieMatchers)
      {
        matcher.StoreCharacterMatch(character);
      }
    }

    public void StoreMovieCompanyMatch(CompanyInfo company)
    {
      foreach (IMovieMatcher matcher in _movieMatchers)
      {
        matcher.StoreCompanyMatch(company);
      }
    }

    #endregion

    #region Series

    public List<SeriesInfo> GetLastChangedSeries()
    {
      List<SeriesInfo> series = new List<SeriesInfo>();
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        foreach (SeriesInfo ser in matcher.GetLastChangedSeries())
          if (!series.Contains(ser))
            series.Add(ser);
      }
      return series;
    }

    public void ResetLastChangedSeries()
    {
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedSeries();
      }
    }

    public List<EpisodeInfo> GetLastChangedEpisodes()
    {
      List<EpisodeInfo> episodes = new List<EpisodeInfo>();
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        foreach (EpisodeInfo episode in matcher.GetLastChangedEpisodes())
          if (!episodes.Contains(episode))
            episodes.Add(episode);
      }
      return episodes;
    }

    public void ResetLastChangedEpisodes()
    {
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedEpisodes();
      }
    }

    public async Task<IEnumerable<EpisodeInfo>> FindMatchingEpisodesAsync(EpisodeInfo episodeInfo)
    {
      var tasks = _seriesMatchers.Where(m => m.Enabled)
        .Select(m => m.FindMatchingEpisodesAsync(episodeInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<IEnumerable<SeriesInfo>> FindMatchingSeriesAsync(SeriesInfo seriesInfo)
    {
      var tasks = _seriesMatchers.Where(m => m.Enabled)
        .Select(m => m.FindMatchingSeriesAsync(seriesInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> FindAndUpdateEpisodeAsync(EpisodeInfo episodeInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.FindAndUpdateEpisodeAsync(episodeInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateEpisodePersonsAsync(EpisodeInfo episodeInfo, string occupation)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateEpisodePersonsAsync(episodeInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateEpisodeCharactersAsync(EpisodeInfo episodeInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateEpisodeCharactersAsync(episodeInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeasonAsync(SeasonInfo seasonInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeasonAsync(seasonInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeriesAsync(SeriesInfo seriesInfo, bool updateEpisodeList)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesAsync(seriesInfo, updateEpisodeList).ConfigureAwait(false);
      }

      if (updateEpisodeList)
      {
        if (seriesInfo.Episodes.Count == 0)
          return false;

        for (int i = 0; i < seriesInfo.Episodes.Count; i++)
        {
          //Gives more detail to the missing episodes but will be very slow
          //EpisodeInfo episodeInfo = seriesInfo.Episodes[i];
          //foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
          //{
          //  success |= matcher.FindAndUpdateEpisode(episodeInfo, importOnly);
          //}
        }
      }
      return success;
    }

    public async Task<bool> UpdateSeriesPersonsAsync(SeriesInfo seriesInfo, string occupation)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesPersonsAsync(seriesInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeriesCharactersAsync(SeriesInfo seriesInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesCharactersAsync(seriesInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeriesCompaniesAsync(SeriesInfo seriesInfo, string companyType)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesCompaniesAsync(seriesInfo, companyType).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> DownloadSeriesFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.DownloadFanArtAsync(mediaItemId, mediaItemInfo).ConfigureAwait(false);
      }
      return success;
    }

    public void StoreSeriesPersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ACTOR)
      {
        foreach (ISeriesMatcher matcher in _seriesMatchers)
        {
          matcher.StoreActorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
      {
        foreach (ISeriesMatcher matcher in _seriesMatchers)
        {
          matcher.StoreDirectorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_WRITER)
      {
        foreach (ISeriesMatcher matcher in _seriesMatchers)
        {
          matcher.StoreWriterMatch(person);
        }
      }
    }

    public void StoreSeriesCharacterMatch(CharacterInfo character)
    {
      foreach (ISeriesMatcher matcher in _seriesMatchers)
      {
        matcher.StoreCharacterMatch(character);
      }
    }

    public void StoreSeriesCompanyMatch(CompanyInfo company)
    {
      if (company.Type == CompanyAspect.COMPANY_PRODUCTION)
      {
        foreach (ISeriesMatcher matcher in _seriesMatchers)
        {
          matcher.StoreCompanyMatch(company);
        }
      }
      else if (company.Type == CompanyAspect.COMPANY_TV_NETWORK)
      {
        foreach (ISeriesMatcher matcher in _seriesMatchers)
        {
          matcher.StoreTvNetworkMatch(company);
        }
      }
    }

    #endregion

    #region Subtitles

    public async Task<IEnumerable<SubtitleInfo>> FindMatchingEpisodeSubtitlesAsync(SubtitleInfo subtitleInfo, List<string> languages)
    {
      var tasks = _subtitleMatchers.Where(m => m.Enabled)
        .Select(m => m.FindMatchingEpisodeSubtitlesAsync(subtitleInfo, languages)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<IEnumerable<SubtitleInfo>> FindMatchingMovieSubtitlesAsync(SubtitleInfo subtitleInfo, List<string> languages)
    {
      var tasks = _subtitleMatchers.Where(m => m.Enabled)
        .Select(m => m.FindMatchingMovieSubtitlesAsync(subtitleInfo, languages)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> DownloadSubtitleAsync(SubtitleInfo subtitleInfo, bool overwriteExisting)
    {
      bool success = false;
      foreach (ISubtitleMatcher matcher in _subtitleMatchers.Where(m => m.Enabled))
      {
        success |= await matcher.DownloadSubtitleAsync(subtitleInfo, overwriteExisting).ConfigureAwait(false);
      }
      return success;
    }

    #endregion

    public static ILogger Logger
    {
      get
      {
        return ServiceRegistration.Get<ILogger>();
      }
    }
  }
}
