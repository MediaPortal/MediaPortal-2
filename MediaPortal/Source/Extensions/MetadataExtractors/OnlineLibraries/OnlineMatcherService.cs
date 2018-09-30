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
  public class OnlineMatcherService : IOnlineMatcherService
  {
    private List<IMusicMatcher> MUSIC_MATCHERS = new List<IMusicMatcher>();
    private List<ISeriesMatcher> SERIES_MATCHERS = new List<ISeriesMatcher>();
    private List<IMovieMatcher> MOVIE_MATCHERS = new List<IMovieMatcher>();
    private SettingsChangeWatcher<OnlineLibrarySettings> SETTINGS_CHANGE_WATCHER = null;

    #region Static instance

    public static IOnlineMatcherService Instance
    {
      get { return ServiceRegistration.Get<IOnlineMatcherService>(); }
    }

    #endregion

    public OnlineMatcherService()
    {
      MUSIC_MATCHERS.Add(MusicTheAudioDbMatcher.Instance);
      MUSIC_MATCHERS.Add(MusicFreeDbMatcher.Instance);
      MUSIC_MATCHERS.Add(MusicBrainzMatcher.Instance);
      MUSIC_MATCHERS.Add(MusicFanArtTvMatcher.Instance);

      MOVIE_MATCHERS.Add(MovieTheMovieDbMatcher.Instance);
      //MOVIE_MATCHERS.Add(MovieOmDbMatcher.Instance);
      MOVIE_MATCHERS.Add(MovieSimApiMatcher.Instance);
      MOVIE_MATCHERS.Add(MovieFanArtTvMatcher.Instance);

      SERIES_MATCHERS.Add(SeriesTvDbMatcher.Instance);
      SERIES_MATCHERS.Add(SeriesTheMovieDbMatcher.Instance);
      SERIES_MATCHERS.Add(SeriesTvMazeMatcher.Instance);
      //SERIES_MATCHERS.Add(SeriesOmDbMatcher.Instance);
      SERIES_MATCHERS.Add(SeriesFanArtTvMatcher.Instance);

      //Load settings
      LoadSettings();

      //Save settings
      SaveSettings();

      SETTINGS_CHANGE_WATCHER = new SettingsChangeWatcher<OnlineLibrarySettings>();
      SETTINGS_CHANGE_WATCHER.SettingsChanged += SettingsChanged;
    }

    private void LoadSettings()
    {
      OnlineLibrarySettings settings = ServiceRegistration.Get<ISettingsManager>().Load<OnlineLibrarySettings>();

      //Music matchers
      ConfigureMatchers(MUSIC_MATCHERS, settings.MusicMatchers, settings.MusicLanguageCulture);

      //Movie matchers
      ConfigureMatchers(MOVIE_MATCHERS, settings.MovieMatchers, settings.MovieLanguageCulture);

      //Series matchers
      ConfigureMatchers(SERIES_MATCHERS, settings.SeriesMatchers, settings.SeriesLanguageCulture);
    }

    protected void ConfigureMatchers<T>(ICollection<T> matchers, ICollection<MatcherSetting> settings, string languageCulture) where T : IMatcher
    {
      foreach (MatcherSetting setting in settings)
      {
        IMatcher matcher = matchers.FirstOrDefault(m => m.Id.Equals(setting.Id, StringComparison.OrdinalIgnoreCase));
        if (matcher != null)
        {
          matcher.Enabled = setting.Enabled;
          matcher.PreferredLanguageCulture = languageCulture;
        }
      }
    }

    private void SaveSettings()
    {
      OnlineLibrarySettings settings = ServiceRegistration.Get<ISettingsManager>().Load<OnlineLibrarySettings>();
      List<MatcherSetting> list = new List<MatcherSetting>();
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS)
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.MusicMatchers = list.ToArray();

      list.Clear();
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS)
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.MovieMatchers = list.ToArray();

      list.Clear();
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS)
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Enabled = matcher.Enabled;
        list.Add(setting);
      }
      settings.SeriesMatchers = list.ToArray();

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

    #region Audio

    public List<AlbumInfo> GetLastChangedAudioAlbums()
    {
      List<AlbumInfo> albums = new List<AlbumInfo>();
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
      {
        foreach (AlbumInfo album in matcher.GetLastChangedAudioAlbums())
          if (!albums.Contains(album))
            albums.Add(album);
      }
      return albums;
    }

    public void ResetLastChangedAudioAlbums()
    {
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedAudioAlbums();
      }
    }

    public List<TrackInfo> GetLastChangedAudio()
    {
      List<TrackInfo> tracks = new List<TrackInfo>();
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
      {
        foreach (TrackInfo track in matcher.GetLastChangedAudio())
          if (!tracks.Contains(track))
            tracks.Add(track);
      }
      return tracks;
    }

    public void ResetLastChangedAudio()
    {
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedAudio();
      }
    }

    public async Task<IEnumerable<TrackInfo>> FindMatchingTracksAsync(TrackInfo trackInfo)
    {
      var tasks = MUSIC_MATCHERS.Where(m => m.Enabled)
        .Select(m => m.FindMatchingTracksAsync(trackInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<IEnumerable<AlbumInfo>> FindMatchingAlbumsAsync(AlbumInfo albumInfo)
    {
      var tasks = MUSIC_MATCHERS.Where(m => m.Enabled)
        .Select(m => m.FindMatchingAlbumsAsync(albumInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> FindAndUpdateTrackAsync(TrackInfo trackInfo)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.FindAndUpdateTrackAsync(trackInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateAlbumPersonsAsync(AlbumInfo albumInfo, string occupation)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateAlbumPersonsAsync(albumInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateTrackPersonsAsync(TrackInfo trackInfo, string occupation, bool forAlbum)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateTrackPersonsAsync(trackInfo, occupation, forAlbum).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateAlbumCompaniesAsync(AlbumInfo albumInfo, string companyType)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateAlbumCompaniesAsync(albumInfo, companyType).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateAlbumAsync(AlbumInfo albumInfo, bool updateTrackList)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
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
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.DownloadFanArtAsync(mediaItemId, mediaItemInfo).ConfigureAwait(false);
      }
      return success;
    }

    public void StoreAudioPersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ARTIST)
      {
        foreach (IMusicMatcher matcher in MUSIC_MATCHERS)
        {
          matcher.StoreArtistMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_COMPOSER)
      {
        foreach (IMusicMatcher matcher in MUSIC_MATCHERS)
        {
          matcher.StoreComposerMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_CONDUCTOR)
      {
        foreach (IMusicMatcher matcher in MUSIC_MATCHERS)
        {
          matcher.StoreConductorMatch(person);
        }
      }
    }

    public void StoreAudioCompanyMatch(CompanyInfo company)
    {
      if (company.Type == CompanyAspect.COMPANY_MUSIC_LABEL)
      {
        foreach (IMusicMatcher matcher in MUSIC_MATCHERS)
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
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
      {
        foreach (MovieInfo movie in matcher.GetLastChangedMovies())
          if (!movies.Contains(movie))
            movies.Add(movie);
      }
      return movies;
    }

    public void ResetLastChangedMovies()
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedMovies();
      }
    }

    public List<MovieCollectionInfo> GetLastChangedMovieCollections()
    {
      List<MovieCollectionInfo> collections = new List<MovieCollectionInfo>();
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
      {
        foreach (MovieCollectionInfo collection in matcher.GetLastChangedMovieCollections())
          if (!collections.Contains(collection))
            collections.Add(collection);
      }
      return collections;
    }

    public void ResetLastChangedMovieCollections()
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedMovieCollections();
      }
    }

    public async Task<IEnumerable<MovieInfo>> FindMatchingMoviesAsync(MovieInfo movieInfo)
    {
      var tasks = MOVIE_MATCHERS.Where(m => m.Enabled)
        .Select(m => m.FindMatchingMoviesAsync(movieInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> FindAndUpdateMovieAsync(MovieInfo movieInfo)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.FindAndUpdateMovieAsync(movieInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdatePersonsAsync(MovieInfo movieInfo, string occupation)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdatePersonsAsync(movieInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateCharactersAsync(MovieInfo movieInfo)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateCharactersAsync(movieInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateCollectionAsync(MovieCollectionInfo collectionInfo, bool updateMovieList)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
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
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateCompaniesAsync(movieInfo, companyType).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> DownloadMovieFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.DownloadFanArtAsync(mediaItemId, mediaItemInfo).ConfigureAwait(false);
      }
      return success;
    }

    public void StoreMoviePersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ACTOR)
      {
        foreach (IMovieMatcher matcher in MOVIE_MATCHERS)
        {
          matcher.StoreActorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
      {
        foreach (IMovieMatcher matcher in MOVIE_MATCHERS)
        {
          matcher.StoreDirectorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_WRITER)
      {
        foreach (IMovieMatcher matcher in MOVIE_MATCHERS)
        {
          matcher.StoreWriterMatch(person);
        }
      }
    }

    public void StoreMovieCharacterMatch(CharacterInfo character)
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS)
      {
        matcher.StoreCharacterMatch(character);
      }
    }

    public void StoreMovieCompanyMatch(CompanyInfo company)
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS)
      {
        matcher.StoreCompanyMatch(company);
      }
    }

    #endregion

    #region Series

    public List<SeriesInfo> GetLastChangedSeries()
    {
      List<SeriesInfo> series = new List<SeriesInfo>();
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        foreach (SeriesInfo ser in matcher.GetLastChangedSeries())
          if (!series.Contains(ser))
            series.Add(ser);
      }
      return series;
    }

    public void ResetLastChangedSeries()
    {
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedSeries();
      }
    }

    public List<EpisodeInfo> GetLastChangedEpisodes()
    {
      List<EpisodeInfo> episodes = new List<EpisodeInfo>();
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        foreach (EpisodeInfo episode in matcher.GetLastChangedEpisodes())
          if (!episodes.Contains(episode))
            episodes.Add(episode);
      }
      return episodes;
    }

    public void ResetLastChangedEpisodes()
    {
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        matcher.ResetLastChangedEpisodes();
      }
    }

    public async Task<IEnumerable<EpisodeInfo>> FindMatchingEpisodesAsync(EpisodeInfo episodeInfo)
    {
      var tasks = SERIES_MATCHERS.Where(m => m.Enabled)
        .Select(m => m.FindMatchingEpisodesAsync(episodeInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<IEnumerable<SeriesInfo>> FindMatchingSeriesAsync(SeriesInfo seriesInfo)
    {
      var tasks = SERIES_MATCHERS.Where(m => m.Enabled)
        .Select(m => m.FindMatchingSeriesAsync(seriesInfo)).ToList();
      //Merge results
      return await MergeResults(tasks).ConfigureAwait(false);
    }

    public async Task<bool> FindAndUpdateEpisodeAsync(EpisodeInfo episodeInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.FindAndUpdateEpisodeAsync(episodeInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateEpisodePersonsAsync(EpisodeInfo episodeInfo, string occupation)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateEpisodePersonsAsync(episodeInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateEpisodeCharactersAsync(EpisodeInfo episodeInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateEpisodeCharactersAsync(episodeInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeasonAsync(SeasonInfo seasonInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeasonAsync(seasonInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeriesAsync(SeriesInfo seriesInfo, bool updateEpisodeList)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
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
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesPersonsAsync(seriesInfo, occupation).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeriesCharactersAsync(SeriesInfo seriesInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesCharactersAsync(seriesInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> UpdateSeriesCompaniesAsync(SeriesInfo seriesInfo, string companyType)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.UpdateSeriesCompaniesAsync(seriesInfo, companyType).ConfigureAwait(false);
      }
      return success;
    }

    public async Task<bool> DownloadSeriesFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.Where(m => m.Enabled))
      {
        success |= await matcher.DownloadFanArtAsync(mediaItemId, mediaItemInfo).ConfigureAwait(false);
      }
      return success;
    }

    public void StoreSeriesPersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ACTOR)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS)
        {
          matcher.StoreActorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS)
        {
          matcher.StoreDirectorMatch(person);
        }
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_WRITER)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS)
        {
          matcher.StoreWriterMatch(person);
        }
      }
    }

    public void StoreSeriesCharacterMatch(CharacterInfo character)
    {
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS)
      {
        matcher.StoreCharacterMatch(character);
      }
    }

    public void StoreSeriesCompanyMatch(CompanyInfo company)
    {
      if (company.Type == CompanyAspect.COMPANY_PRODUCTION)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS)
        {
          matcher.StoreCompanyMatch(company);
        }
      }
      else if (company.Type == CompanyAspect.COMPANY_TV_NETWORK)
      {
        foreach (ISeriesMatcher matcher in SERIES_MATCHERS)
        {
          matcher.StoreTvNetworkMatch(company);
        }
      }
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
