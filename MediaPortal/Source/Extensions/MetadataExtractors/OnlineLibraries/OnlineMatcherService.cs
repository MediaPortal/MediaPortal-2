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
using MediaPortal.Common.Genres;
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
using System.Text.RegularExpressions;

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
    private List<GenreMapping> MUSIC_GENRE_MAP = new List<GenreMapping>();
    private List<GenreMapping> SERIES_GENRE_MAP = new List<GenreMapping>();
    private List<GenreMapping> MOVIE_GENRE_MAP = new List<GenreMapping>();
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
      MUSIC_MATCHERS.Add(CDFreeDbMatcher.Instance);
      MUSIC_MATCHERS.Add(MusicBrainzMatcher.Instance);
      MUSIC_MATCHERS.Add(MusicFanArtTvMatcher.Instance);

      MOVIE_MATCHERS.Add(MovieTheMovieDbMatcher.Instance);
      MOVIE_MATCHERS.Add(MovieOmDbMatcher.Instance);
      MOVIE_MATCHERS.Add(MovieFanArtTvMatcher.Instance);

      SERIES_MATCHERS.Add(SeriesTvDbMatcher.Instance);
      SERIES_MATCHERS.Add(SeriesTheMovieDbMatcher.Instance);
      SERIES_MATCHERS.Add(SeriesTvMazeMatcher.Instance);
      SERIES_MATCHERS.Add(SeriesOmDbMatcher.Instance);
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
      foreach (MatcherSetting setting in settings.MusicMatchers)
      {
        IMusicMatcher matcher = MUSIC_MATCHERS.Find(m => m.Id.Equals(setting.Id, StringComparison.InvariantCultureIgnoreCase));
        if (matcher != null)
        {
          matcher.Primary = setting.Primary;
          matcher.Enabled = setting.Enabled;
          matcher.PreferredLanguageCulture = settings.MusicLanguageCulture;
        }
      }
      if (settings.MusicGenreMappings.Length == 0)
      {
        settings.MusicGenreMappings = new GenreMapping[]
        {
          new GenreMapping(MusicGenre.CLASSIC, new SerializableRegex(@"Classic|Opera|Orchestral|Choral|Avant|Baroque|Chant", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.SOUNDTRACK, new SerializableRegex(@"Soundtrack|Cinema|Musical|Score", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.NEW_AGE, new SerializableRegex(@"New Age|Environment|Healing|Meditation|Nature|Relax|Travel", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.ROCK, new SerializableRegex(@"Rock|Grunge|Punk", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.METAL, new SerializableRegex(@"Metal", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.COUNTRY, new SerializableRegex(@"Country|Americana|Bluegrass|Cowboy|Honky|Hokum", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.JAZZ, new SerializableRegex(@"Jazz|Big Band|Fusion|Ragtime", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.RB_SOUL, new SerializableRegex(@"R&B|Soul|Disco|Funk|Swing|Blues", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.HIP_HOP_RAP, new SerializableRegex(@"Hop|Rap|Bounce|Turntablism", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.RAGGAE, new SerializableRegex(@"Reggae|Dancehall", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.POP, new SerializableRegex(@"Pop|Beat", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.DANCE, new SerializableRegex(@"Dance|Club|House|Step|Garage|Trance|NRG|Core|Techno", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.ELECTRONIC, new SerializableRegex(@"Electronic|Electro|Experimental|8bit|Chiptune|Downtempo|Industrial", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.COMEDY, new SerializableRegex(@"Comedy|Novelty|Parody", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.FOLK, new SerializableRegex(@"Folk", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.EASY_LISTENING, new SerializableRegex(@"Easy|Lounge|Background|Swing|Bop|Ambient", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.HOLIDAY, new SerializableRegex(@"Holiday|Chanukah|Christmas|Easter|Halloween|Thanksgiving", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.WORLD, new SerializableRegex(@"World|Africa|Afro|Asia|Australia|Cajun|Latin|Calypso|Caribbean|Celtic|Europe|France|America|Polka|Japanese|Indian|Korean|German|Danish|Ballad|Ethnic|Indie", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.ALTERNATIVE, new SerializableRegex(@"Alternative|New Wave|Progressive", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.COMPILATION, new SerializableRegex(@"Compilation|Top", RegexOptions.IgnoreCase)),
        };
      }
      MUSIC_GENRE_MAP = new List<GenreMapping>(settings.MusicGenreMappings);

      foreach (MatcherSetting setting in settings.MovieMatchers)
      {
        IMovieMatcher matcher = MOVIE_MATCHERS.Find(m => m.Id.Equals(setting.Id, StringComparison.InvariantCultureIgnoreCase));
        if (matcher != null)
        {
          matcher.Primary = setting.Primary;
          matcher.Enabled = setting.Enabled;
          matcher.PreferredLanguageCulture = settings.MovieLanguageCulture;
        }
      }
      if (settings.MovieGenreMappings.Length == 0)
      {
        settings.MovieGenreMappings = new GenreMapping[]
        {
          new GenreMapping(MovieGenre.ACTION, new SerializableRegex(@"Action", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.ADVENTURE, new SerializableRegex(@"Adventure", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.ANIMATION, new SerializableRegex(@"Animation|Cartoon|Anime", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.COMEDY, new SerializableRegex(@"Comedy", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.CRIME, new SerializableRegex(@"Crime", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.DOCUMENTARY, new SerializableRegex(@"Documentary|Biography", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.DRAMA, new SerializableRegex(@"Drama", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.FAMILY, new SerializableRegex(@"Family", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.FANTASY, new SerializableRegex(@"Fantasy", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.HISTORY, new SerializableRegex(@"History", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.HORROR, new SerializableRegex(@"Horror", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.MUSIC, new SerializableRegex(@"Music", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.MYSTERY, new SerializableRegex(@"Mystery", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.ROMANCE, new SerializableRegex(@"Romance", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.SCIENCE_FICTION, new SerializableRegex(@"Science Fiction|Science-Fiction|Sci-Fi", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.TV_MOVIE, new SerializableRegex(@"TV", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.THRILLER, new SerializableRegex(@"Thriller|Disaster|Suspense", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.WAR, new SerializableRegex(@"War", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.WESTERN, new SerializableRegex(@"Western", RegexOptions.IgnoreCase)),
        };
      }
      MOVIE_GENRE_MAP = new List<GenreMapping>(settings.MovieGenreMappings);

      foreach (MatcherSetting setting in settings.SeriesMatchers)
      {
        ISeriesMatcher matcher = SERIES_MATCHERS.Find(m => m.Id.Equals(setting.Id, StringComparison.InvariantCultureIgnoreCase));
        if (matcher != null)
        {
          matcher.Primary = setting.Primary;
          matcher.Enabled = setting.Enabled;
          matcher.PreferredLanguageCulture = settings.SeriesLanguageCulture;
        }
      }
      if (settings.SeriesGenreMappings.Length == 0)
      {
        settings.SeriesGenreMappings = new GenreMapping[]
        {
          new GenreMapping(SeriesGenre.ACTION, new SerializableRegex(@"Action", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.ADVENTURE, new SerializableRegex(@"Adventure", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.ANIMATION, new SerializableRegex(@"Animation|Cartoon|Anime", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.COMEDY, new SerializableRegex(@"Comedy", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.CRIME, new SerializableRegex(@"Crime", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.DOCUMENTARY, new SerializableRegex(@"Documentary|Biography", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.DRAMA, new SerializableRegex(@"Drama", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.FAMILY, new SerializableRegex(@"Family", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.FANTASY, new SerializableRegex(@"Fantasy", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.HISTORY, new SerializableRegex(@"History", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.HORROR, new SerializableRegex(@"Horror", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.MUSIC, new SerializableRegex(@"Music", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.MYSTERY, new SerializableRegex(@"Mystery", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.ROMANCE, new SerializableRegex(@"Romance", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.SCIENCE_FICTION, new SerializableRegex(@"Science Fiction|Science-Fiction|Sci-Fi", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.THRILLER, new SerializableRegex(@"Thriller|Disaster|Suspense", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.WAR, new SerializableRegex(@"War", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.WESTERN, new SerializableRegex(@"Western", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.KIDS, new SerializableRegex(@"Kids|Children|Teen", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.NEWS, new SerializableRegex(@"News", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.REALITY, new SerializableRegex(@"Reality", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.SOAP, new SerializableRegex(@"Soap", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.TALK, new SerializableRegex(@"Talk", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.POLITICS, new SerializableRegex(@"Politic", RegexOptions.IgnoreCase)),
        };
      }
      SERIES_GENRE_MAP = new List<GenreMapping>(settings.SeriesGenreMappings);
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
        setting.Primary = matcher.Primary;
        list.Add(setting);
      }
      settings.MusicMatchers = list.ToArray();
      settings.MusicGenreMappings = MUSIC_GENRE_MAP.ToArray();

      list.Clear();
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS)
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Enabled = matcher.Enabled;
        setting.Primary = matcher.Primary;
        list.Add(setting);
      }
      settings.MovieMatchers = list.ToArray();
      settings.MovieGenreMappings = MOVIE_GENRE_MAP.ToArray();

      list.Clear();
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS)
      {
        MatcherSetting setting = new MatcherSetting();
        setting.Id = matcher.Id;
        setting.Enabled = matcher.Enabled;
        setting.Primary = matcher.Primary;
        list.Add(setting);
      }
      settings.SeriesMatchers = list.ToArray();
      settings.SeriesGenreMappings = SERIES_GENRE_MAP.ToArray();

      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    private bool AssignMissingGenreIds(List<GenreInfo> genres, List<GenreMapping> genreMap)
    {
      bool retVal = false;
      List<GenreInfo> checkGenres = new List<GenreInfo>(genres);
      genres.Clear();
      foreach (GenreInfo genre in checkGenres)
      {
        if (genre.Id > 0)
        {
          if (!genres.Contains(genre))
            genres.Add(genre);
          continue;
        }

        if (string.IsNullOrEmpty(genre.Name))
          continue;

        GenreInfo testGenre = genre;
        foreach (GenreMapping map in genreMap)
        {
          if (map.GenrePattern.Regex.IsMatch(genre.Name))
          {
            testGenre = new GenreInfo
            {
              Id = map.GenreId,
              Name = genre.Name
            };
            retVal = true;
            break;
          }
        }
        if (!genres.Contains(testGenre))
          genres.Add(testGenre);
      }
      return retVal;
    }

    #region Audio

    public bool AssignMissingMusicGenreIds(List<GenreInfo> genres)
    {
      return AssignMissingGenreIds(genres, MUSIC_GENRE_MAP);
    }

    public List<AlbumInfo> GetLastChangedAudioAlbums()
    {
      List<AlbumInfo> albums = new List<AlbumInfo>();
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        foreach (AlbumInfo album in matcher.GetLastChangedAudioAlbums())
          if (!albums.Contains(album))
            albums.Add(album);
      }
      return albums;
    }

    public void ResetLastChangedAudioAlbums()
    {
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedAudioAlbums();
      }
    }

    public List<TrackInfo> GetLastChangedAudio()
    {
      List<TrackInfo> tracks = new List<TrackInfo>();
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        foreach (TrackInfo track in matcher.GetLastChangedAudio())
          if (!tracks.Contains(track))
            tracks.Add(track);
      }
      return tracks;
    }

    public void ResetLastChangedAudio()
    {
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedAudio();
      }
    }

    public bool FindAndUpdateTrack(TrackInfo trackInfo, bool importOnly)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.FindAndUpdateTrack(trackInfo, matcher.Primary ? false : importOnly);
      }
      return success;
    }

    public bool UpdateAlbumPersons(AlbumInfo albumInfo, string occupation, bool importOnly)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateAlbumPersons(albumInfo, occupation, importOnly);
      }
      return success;
    }

    public bool UpdateTrackPersons(TrackInfo trackInfo, string occupation, bool forAlbum, bool importOnly)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateTrackPersons(trackInfo, occupation, forAlbum, importOnly);
      }
      return success;
    }

    public bool UpdateAlbumCompanies(AlbumInfo albumInfo, string companyType, bool importOnly)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateAlbumCompanies(albumInfo, companyType, importOnly);
      }
      return success;
    }

    public bool UpdateAlbum(AlbumInfo albumInfo, bool updateTrackList, bool importOnly)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateAlbum(albumInfo, updateTrackList, matcher.Primary ? false : importOnly);
      }

      if (updateTrackList)
      {
        if (albumInfo.Tracks.Count == 0)
          return false;

        for (int i = 0; i < albumInfo.Tracks.Count; i++)
        {
          //TrackInfo trackInfo = albumInfo.Tracks[i];
          //foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
          //{
          //  matcher.FindAndUpdateTrack(trackInfo, importOnly);
          //}
        }
      }
      return success;
    }

    public bool DownloadAudioFanArt(Guid mediaItemId, BaseInfo mediaItemInfo, bool force)
    {
      bool success = false;
      foreach (IMusicMatcher matcher in MUSIC_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.ScheduleFanArtDownload(mediaItemId, mediaItemInfo, force);
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

    public bool AssignMissingMovieGenreIds(List<GenreInfo> genres)
    {
      return AssignMissingGenreIds(genres, MOVIE_GENRE_MAP);
    }

    public List<MovieInfo> GetLastChangedMovies()
    {
      List<MovieInfo> movies = new List<MovieInfo>();
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        foreach (MovieInfo movie in matcher.GetLastChangedMovies())
          if (!movies.Contains(movie))
            movies.Add(movie);
      }
      return movies;
    }

    public void ResetLastChangedMovies()
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedMovies();
      }
    }

    public List<MovieCollectionInfo> GetLastChangedMovieCollections()
    {
      List<MovieCollectionInfo> collections = new List<MovieCollectionInfo>();
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        foreach (MovieCollectionInfo collection in matcher.GetLastChangedMovieCollections())
          if (!collections.Contains(collection))
            collections.Add(collection);
      }
      return collections;
    }

    public void ResetLastChangedMovieCollections()
    {
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedMovieCollections();
      }
    }

    public bool FindAndUpdateMovie(MovieInfo movieInfo, bool importOnly)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.FindAndUpdateMovie(movieInfo, matcher.Primary ? false : importOnly);
      }
      return success;
    }

    public bool UpdatePersons(MovieInfo movieInfo, string occupation, bool importOnly)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdatePersons(movieInfo, occupation, importOnly);
      }
      return success;
    }

    public bool UpdateCharacters(MovieInfo movieInfo, bool importOnly)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateCharacters(movieInfo, importOnly);
      }
      return success;
    }

    public bool UpdateCollection(MovieCollectionInfo collectionInfo, bool updateMovieList, bool importOnly)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateCollection(collectionInfo, updateMovieList, importOnly);
      }

      if (updateMovieList)
      {
        if (collectionInfo.Movies.Count == 0)
          return false;

        for (int i = 0; i < collectionInfo.Movies.Count; i++)
        {
          //MovieInfo movieInfo = collectionInfo.Movies[i];
          //foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
          //{
          //  success |= matcher.FindAndUpdateMovie(movieInfo, importOnly);
          //}
        }
      }
      return success;
    }

    public bool UpdateCompanies(MovieInfo movieInfo, string companyType, bool importOnly)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateCompanies(movieInfo, companyType, importOnly);
      }
      return success;
    }

    public bool DownloadMovieFanArt(Guid mediaItemId, BaseInfo mediaItemInfo, bool force)
    {
      bool success = false;
      foreach (IMovieMatcher matcher in MOVIE_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.ScheduleFanArtDownload(mediaItemId, mediaItemInfo, force);
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

    public bool AssignMissingSeriesGenreIds(List<GenreInfo> genres)
    {
      return AssignMissingGenreIds(genres, SERIES_GENRE_MAP);
    }

    public List<SeriesInfo> GetLastChangedSeries()
    {
      List<SeriesInfo> series = new List<SeriesInfo>();
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        foreach (SeriesInfo ser in matcher.GetLastChangedSeries())
          if (!series.Contains(ser))
            series.Add(ser);
      }
      return series;
    }

    public void ResetLastChangedSeries()
    {
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedSeries();
      }
    }

    public List<EpisodeInfo> GetLastChangedEpisodes()
    {
      List<EpisodeInfo> episodes = new List<EpisodeInfo>();
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        foreach (EpisodeInfo episode in matcher.GetLastChangedEpisodes())
          if (!episodes.Contains(episode))
            episodes.Add(episode);
      }
      return episodes;
    }

    public void ResetLastChangedEpisodes()
    {
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        matcher.ResetLastChangedEpisodes();
      }
    }

    public bool FindAndUpdateEpisode(EpisodeInfo episodeInfo, bool importOnly)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.FindAndUpdateEpisode(episodeInfo, matcher.Primary ? false : importOnly);
      }
      return success;
    }

    public bool UpdateEpisodePersons(EpisodeInfo episodeInfo, string occupation, bool importOnly)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateEpisodePersons(episodeInfo, occupation, importOnly);
      }
      return success;
    }

    public bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo, bool importOnly)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateEpisodeCharacters(episodeInfo, importOnly);
      }
      return success;
    }

    public bool UpdateSeason(SeasonInfo seasonInfo, bool importOnly)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateSeason(seasonInfo, importOnly);
      }
      return success;
    }

    public bool UpdateSeries(SeriesInfo seriesInfo, bool updateEpisodeList, bool importOnly)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateSeries(seriesInfo, updateEpisodeList, matcher.Primary ? false : importOnly);
      }

      if (updateEpisodeList)
      {
        if (seriesInfo.Episodes.Count == 0)
          return false;

        for (int i = 0; i < seriesInfo.Episodes.Count; i++)
        {
          //Gives more detail to the missing episodes but will be very slow
          //EpisodeInfo episodeInfo = seriesInfo.Episodes[i];
          //foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
          //{
          //  success |= matcher.FindAndUpdateEpisode(episodeInfo, importOnly);
          //}
        }
      }
      return success;
    }

    public bool UpdateSeriesPersons(SeriesInfo seriesInfo, string occupation, bool importOnly)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateSeriesPersons(seriesInfo, occupation, importOnly);
      }
      return success;
    }

    public bool UpdateSeriesCharacters(SeriesInfo seriesInfo, bool importOnly)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateSeriesCharacters(seriesInfo, importOnly);
      }
      return success;
    }

    public bool UpdateSeriesCompanies(SeriesInfo seriesInfo, string companyType, bool importOnly)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.UpdateSeriesCompanies(seriesInfo, companyType, importOnly);
      }
      return success;
    }

    public bool DownloadSeriesFanArt(Guid mediaItemId, BaseInfo mediaItemInfo, bool force)
    {
      bool success = false;
      foreach (ISeriesMatcher matcher in SERIES_MATCHERS.OrderByDescending(m => m.Primary).Where(m => m.Enabled))
      {
        success |= matcher.ScheduleFanArtDownload(mediaItemId, mediaItemInfo, force);
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
