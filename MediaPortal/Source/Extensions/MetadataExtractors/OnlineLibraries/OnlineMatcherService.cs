#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Libraries;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using System;
using System.Collections.Concurrent;
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
    private static bool ALLOW_MATCHER_REGISTRATION = true;

    private static readonly ConcurrentDictionary<string, ConcurrentBag<IAudioMatcher>> AUDIO_MATCHERS = new ConcurrentDictionary<string, ConcurrentBag<IAudioMatcher>>();
    private static readonly ConcurrentDictionary<string, ConcurrentBag<ISeriesMatcher>> SERIES_MATCHERS = new ConcurrentDictionary<string, ConcurrentBag<ISeriesMatcher>>();
    private static readonly ConcurrentDictionary<string, ConcurrentBag<IMovieMatcher>> MOVIE_MATCHERS = new ConcurrentDictionary<string, ConcurrentBag<IMovieMatcher>>();
    private static readonly ConcurrentDictionary<string, ConcurrentBag<ISubtitleMatcher>> SUBTITLE_MATCHERS = new ConcurrentDictionary<string, ConcurrentBag<ISubtitleMatcher>>();

    private SettingsChangeWatcher<OnlineLibrarySettings> _settingChangeWatcher = null;
    private AsynchronousMessageQueue _messageQueue;

    #region Static instance

    public static IOnlineMatcherService Instance
    {
      get { return ServiceRegistration.Get<IOnlineMatcherService>(); }
    }


    public static void RegisterDefaultAudioMatchers(string category)
    {
      RegisterAudioMatchers(new IAudioMatcher[]
      {
        new MusicTheAudioDbMatcher(),
        new MusicFreeDbMatcher(),
        new MusicBrainzMatcher(),
        new MusicFanArtTvMatcher()
      }, category);
    }

    public static void RegisterAudioMatchers(IAudioMatcher[] matchers, string category)
    {
      if (!ALLOW_MATCHER_REGISTRATION)
        throw new InvalidOperationException("Registering of audio matchers is no longer allowed");

      var list = AUDIO_MATCHERS.GetOrAdd(category, (c) => new ConcurrentBag<IAudioMatcher>());
      foreach (var m in matchers)
      {
        if (!list.Contains(m))
          list.Add(m);
      }
    }


    public static void RegisterDefaultMovieMatchers(string category)
    {
      RegisterMovieMatchers(new IMovieMatcher[]
      {
        new MovieTheMovieDbMatcher(),
        //new MovieOmDbMatcher(),
        new MovieSimApiMatcher(),
        new MovieFanArtTvMatcher()
      }, category);
    }

    public static void RegisterMovieMatchers(IMovieMatcher[] matchers, string category)
    {
      if (!ALLOW_MATCHER_REGISTRATION)
        throw new InvalidOperationException("Registering of movie matchers is no longer allowed");

      var list = MOVIE_MATCHERS.GetOrAdd(category, (c) => new ConcurrentBag<IMovieMatcher>());
      foreach (var m in matchers)
      {
        if (!list.Contains(m))
          list.Add(m);
      }
    }


    public static void RegisterDefaultSeriesMatchers(string category)
    {
      RegisterSeriesMatchers(new ISeriesMatcher[]
      {
        new SeriesTvDbMatcher(),
        new SeriesTheMovieDbMatcher(),
        new SeriesTvMazeMatcher(),
        //new SeriesOmDbMatcher(),
        new SeriesFanArtTvMatcher()
      }, category);
    }

    public static void RegisterSeriesMatchers(ISeriesMatcher[] matchers, string category)
    {
      if (!ALLOW_MATCHER_REGISTRATION)
        throw new InvalidOperationException("Registering of series matchers is no longer allowed");

      var list = SERIES_MATCHERS.GetOrAdd(category, (c) => new ConcurrentBag<ISeriesMatcher>());
      foreach (var m in matchers)
      {
        if (!list.Contains(m))
          list.Add(m);
      }
    }


    public static void RegisterDefaultMovieSubtitleMatchers(string category)
    {
      RegisterSubtitleMatchers(new ISubtitleMatcher[]
      {
        new SubDbMatcher(),
        new SubsMaxMatcher()
      }, category);
    }

    public static void RegisterDefaultSeriesSubtitleMatchers(string category)
    {
      RegisterSubtitleMatchers(new ISubtitleMatcher[]
      {
        new SubDbMatcher(),
        new SubsMaxMatcher()
      }, category);
    }

    public static void RegisterSubtitleMatchers(ISubtitleMatcher[] matchers, string category)
    {
      if (!ALLOW_MATCHER_REGISTRATION)
        throw new InvalidOperationException("Registering of subtitle matchers is no longer allowed");

      var list = SUBTITLE_MATCHERS.GetOrAdd(category, (c) => new ConcurrentBag<ISubtitleMatcher>());
      foreach (var m in matchers)
      {
        if (!list.Contains(m))
          list.Add(m);
      }
    }

    #endregion

    public OnlineMatcherService()
    {
      _settingChangeWatcher = new SettingsChangeWatcher<OnlineLibrarySettings>();
      _settingChangeWatcher.SettingsChanged += SettingsChanged;

      _messageQueue = new AsynchronousMessageQueue(this, new[]
        {
          SystemMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void LoadSettings()
    {
      OnlineLibrarySettings settings = ServiceRegistration.Get<ISettingsManager>().Load<OnlineLibrarySettings>();

      //Music matchers
      ConfigureMatchers(AUDIO_MATCHERS.SelectMany(m => m.Value), settings.MusicMatchers, settings.MusicLanguageCulture, settings.UseMusicAudioLanguageIfUnmatched);

      //Movie matchers
      ConfigureMatchers(MOVIE_MATCHERS.SelectMany(m => m.Value), settings.MovieMatchers, settings.MovieLanguageCulture, settings.UseMovieAudioLanguageIfUnmatched);

      //Series matchers
      ConfigureMatchers(SERIES_MATCHERS.SelectMany(m => m.Value), settings.SeriesMatchers, settings.SeriesLanguageCulture, settings.UseSeriesAudioLanguageIfUnmatched);

      //Subtitle matchers
      ConfigureMatchers(SUBTITLE_MATCHERS.SelectMany(m => m.Value), settings.SubtitleMatchers, settings.SubtitleLanguageCulture, false);
    }

    protected void ConfigureMatchers<T>(IEnumerable<T> matchers, ICollection<MatcherSetting> settings, string languageCulture, bool useMediaAudioIfUnmatched) where T : IMatcher
    {
      foreach (MatcherSetting setting in settings)
      {
        var mtchs = matchers.Where(m => m.Id.Equals(setting.Id, StringComparison.OrdinalIgnoreCase));
        foreach(var m in mtchs)
        {
          m.Enabled = setting.Enabled;
          m.PreferredLanguageCulture = languageCulture;
          m.UseMediaAudioIfUnmatched = useMediaAudioIfUnmatched;
        }
      }
    }

    private void SaveSettings()
    {
      OnlineLibrarySettings settings = ServiceRegistration.Get<ISettingsManager>().Load<OnlineLibrarySettings>();
      List<MatcherSetting> list = new List<MatcherSetting>();
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Distinct())
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Name = matcher.Name;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.MusicMatchers = list.ToArray();

      list.Clear();
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Distinct())
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Name = matcher.Name;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.MovieMatchers = list.ToArray();

      list.Clear();
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Distinct())
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Name = matcher.Name;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.SeriesMatchers = list.ToArray();

      list.Clear();
      foreach (ISubtitleMatcher matcher in SUBTITLE_MATCHERS.SelectMany(m => m.Value).Distinct())
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

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        var messageType = (SystemMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            var newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.Running)
            {
              //Matchers registered after this point would not be guaranteed to be included in a pending import
              ALLOW_MATCHER_REGISTRATION = false;

              //Load settings for all matchers which should all be registered at this point
              LoadSettings();

              //Save settings so they are available for setup in the client
              SaveSettings();
            }
            else if (newState == SystemState.ShuttingDown)
            {
              //Save settings in case any matchers were changed after startup
              SaveSettings();
            }
            break;
        }
      }
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

    public void Dispose()
    {
      foreach (IDisposable result in AUDIO_MATCHERS.OfType<IDisposable>())
        result.Dispose();
      foreach (IDisposable result in MOVIE_MATCHERS.OfType<IDisposable>())
        result.Dispose();
      foreach (IDisposable result in SERIES_MATCHERS.OfType<IDisposable>())
        result.Dispose();
      foreach (IDisposable result in SUBTITLE_MATCHERS.OfType<IDisposable>())
        result.Dispose();
    }

    #endregion

    #region Audio

    public List<AlbumInfo> GetLastChangedAudioAlbums()
    {
      List<AlbumInfo> albums = new List<AlbumInfo>();
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        foreach (AlbumInfo album in matcher.GetLastChangedAudioAlbums())
          if (!albums.Contains(album))
            albums.Add(album);
      }
      return albums;
    }

    public void ResetLastChangedAudioAlbums()
    {
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedAudioAlbums();
      }
    }

    public List<TrackInfo> GetLastChangedAudio()
    {
      List<TrackInfo> tracks = new List<TrackInfo>();
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        foreach (TrackInfo track in matcher.GetLastChangedAudio())
          if (!tracks.Contains(track))
            tracks.Add(track);
      }
      return tracks;
    }

    public void ResetLastChangedAudio()
    {
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedAudio();
      }
    }

    public async Task<IEnumerable<TrackInfo>> FindMatchingTracksAsync(TrackInfo trackInfo, string category)
    {
      var tasks = AUDIO_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled)
        .Select(m => m.FindMatchingTracksAsync(trackInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<IEnumerable<AlbumInfo>> FindMatchingAlbumsAsync(AlbumInfo albumInfo, string category)
    {
      var tasks = AUDIO_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled)
        .Select(m => m.FindMatchingAlbumsAsync(albumInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> FindAndUpdateTrackAsync(TrackInfo trackInfo, string category)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.FindAndUpdateTrackAsync(trackInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateAlbumPersonsAsync(AlbumInfo albumInfo, string occupation, string category)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateAlbumPersonsAsync(albumInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateTrackPersonsAsync(TrackInfo trackInfo, string occupation, bool forAlbum, string category)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateTrackPersonsAsync(trackInfo, occupation, forAlbum).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateAlbumCompaniesAsync(AlbumInfo albumInfo, string companyType, string category)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateAlbumCompaniesAsync(albumInfo, companyType).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateAlbumAsync(AlbumInfo albumInfo, bool updateTrackList, string category)
    {
      bool success = false;
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
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
          //foreach (IMusicMatcher matcher in _audioMatchers.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
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
      foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.DownloadFanArtAsync(mediaItemId, mediaItemInfo).ConfigureAwait(false);
      }
      return success;
    }

    public void StoreAudioPersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ARTIST)
      {
        foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreArtistMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_COMPOSER)
      {
        foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreComposerMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_CONDUCTOR)
      {
        foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreConductorMatch(person);
        }
      }
    }

    public void StoreAudioCompanyMatch(CompanyInfo company)
    {
      if (company.Type == CompanyAspect.COMPANY_MUSIC_LABEL)
      {
        foreach (IAudioMatcher matcher in AUDIO_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
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
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        foreach (MovieInfo movie in matcher.GetLastChangedMovies())
          if (!movies.Contains(movie))
            movies.Add(movie);
      }
      return movies;
    }

    public void ResetLastChangedMovies()
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedMovies();
      }
    }

    public List<MovieCollectionInfo> GetLastChangedMovieCollections()
    {
      List<MovieCollectionInfo> collections = new List<MovieCollectionInfo>();
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        foreach (MovieCollectionInfo collection in matcher.GetLastChangedMovieCollections())
          if (!collections.Contains(collection))
            collections.Add(collection);
      }
      return collections;
    }

    public void ResetLastChangedMovieCollections()
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedMovieCollections();
      }
    }

    public async Task<IEnumerable<MovieInfo>> FindMatchingMoviesAsync(MovieInfo movieInfo, string category)
    {
      var tasks = MOVIE_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled)
        .Select(m => m.FindMatchingMoviesAsync(movieInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> FindAndUpdateMovieAsync(MovieInfo movieInfo, string category)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.FindAndUpdateMovieAsync(movieInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdatePersonsAsync(MovieInfo movieInfo, string occupation, string category)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdatePersonsAsync(movieInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateCharactersAsync(MovieInfo movieInfo, string category)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateCharactersAsync(movieInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateCollectionAsync(MovieCollectionInfo collectionInfo, bool updateMovieList, string category)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
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
          //foreach (IMovieMatcher matcher in _movieMatchers.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
          //{
          //  success |= matcher.FindAndUpdateMovie(movieInfo, importOnly);
          //}
        }
      }
      return success;
    }

    public async Task<bool> UpdateCompaniesAsync(MovieInfo movieInfo, string companyType, string category)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateCompaniesAsync(movieInfo, companyType).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> DownloadMovieFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.DownloadFanArtAsync(mediaItemId, mediaItemInfo).ConfigureAwait(false);
      }
      return success;
    }

    public void StoreMoviePersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ACTOR)
      {
        foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreActorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
      {
        foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreDirectorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_WRITER)
      {
        foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreWriterMatch(person);
        }
      }
    }

    public void StoreMovieCharacterMatch(CharacterInfo character)
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        matcher.StoreCharacterMatch(character);
      }
    }

    public void StoreMovieCompanyMatch(CompanyInfo company)
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        matcher.StoreCompanyMatch(company);
      }
    }

    #endregion

    #region Series

    public List<SeriesInfo> GetLastChangedSeries()
    {
      List<SeriesInfo> series = new List<SeriesInfo>();
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        foreach (SeriesInfo ser in matcher.GetLastChangedSeries())
          if (!series.Contains(ser))
            series.Add(ser);
      }
      return series;
    }

    public void ResetLastChangedSeries()
    {
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedSeries();
      }
    }

    public List<EpisodeInfo> GetLastChangedEpisodes()
    {
      List<EpisodeInfo> episodes = new List<EpisodeInfo>();
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        foreach (EpisodeInfo episode in matcher.GetLastChangedEpisodes())
          if (!episodes.Contains(episode))
            episodes.Add(episode);
      }
      return episodes;
    }

    public void ResetLastChangedEpisodes()
    {
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedEpisodes();
      }
    }

    public async Task<IEnumerable<EpisodeInfo>> FindMatchingEpisodesAsync(EpisodeInfo episodeInfo, string category)
    {
      var tasks = SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled)
        .Select(m => m.FindMatchingEpisodesAsync(episodeInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<IEnumerable<SeriesInfo>> FindMatchingSeriesAsync(SeriesInfo seriesInfo, string category)
    {
      var tasks = SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled)
        .Select(m => m.FindMatchingSeriesAsync(seriesInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> FindAndUpdateEpisodeAsync(EpisodeInfo episodeInfo, string category)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.FindAndUpdateEpisodeAsync(episodeInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateEpisodePersonsAsync(EpisodeInfo episodeInfo, string occupation, string category)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateEpisodePersonsAsync(episodeInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateEpisodeCharactersAsync(EpisodeInfo episodeInfo, string category)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateEpisodeCharactersAsync(episodeInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeasonAsync(SeasonInfo seasonInfo, string category)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeasonAsync(seasonInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeriesAsync(SeriesInfo seriesInfo, bool updateEpisodeList, string category)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
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
          //foreach (ISeriesMatcher matcher in _seriesMatchers.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
          //{
          //  success |= matcher.FindAndUpdateEpisode(episodeInfo, importOnly);
          //}
        }
      }
      return success;
    }

    public async Task<bool> UpdateSeriesPersonsAsync(SeriesInfo seriesInfo, string occupation, string category)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesPersonsAsync(seriesInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeriesCharactersAsync(SeriesInfo seriesInfo, string category)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesCharactersAsync(seriesInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeriesCompaniesAsync(SeriesInfo seriesInfo, string companyType, string category)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesCompaniesAsync(seriesInfo, companyType).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> DownloadSeriesFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        success |= await matcher.DownloadFanArtAsync(mediaItemId, mediaItemInfo).ConfigureAwait(false);
      }
      return success;
    }

    public void StoreSeriesPersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ACTOR)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreActorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreDirectorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_WRITER)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreWriterMatch(person);
        }
      }
    }

    public void StoreSeriesCharacterMatch(CharacterInfo character)
    {
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
      {
        matcher.StoreCharacterMatch(character);
      }
    }

    public void StoreSeriesCompanyMatch(CompanyInfo company)
    {
      if (company.Type == CompanyAspect.COMPANY_PRODUCTION)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreCompanyMatch(company);
        }
      }
      else if (company.Type == CompanyAspect.COMPANY_TV_NETWORK)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS.SelectMany(m => m.Value).Where(m => m.Enabled))
        {
          matcher.StoreTvNetworkMatch(company);
        }
      }
    }

    #endregion

    #region Subtitles

    public async Task<IEnumerable<SubtitleInfo>> FindMatchingEpisodeSubtitlesAsync(SubtitleInfo subtitleInfo, List<string> languages, string category)
    {
      var tasks = SUBTITLE_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled)
        .Select(m => m.FindMatchingEpisodeSubtitlesAsync(subtitleInfo, languages)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<IEnumerable<SubtitleInfo>> FindMatchingMovieSubtitlesAsync(SubtitleInfo subtitleInfo, List<string> languages, string category)
    {
      var tasks = SUBTITLE_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled)
        .Select(m => m.FindMatchingMovieSubtitlesAsync(subtitleInfo, languages)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> DownloadSubtitleAsync(SubtitleInfo subtitleInfo, bool overwriteExisting, string category = null)
    {
      bool success = false;
      foreach (ISubtitleMatcher matcher in SUBTITLE_MATCHERS.Where(m => category == null || m.Key == category).SelectMany(m => m.Value).Where(m => m.Enabled))
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
