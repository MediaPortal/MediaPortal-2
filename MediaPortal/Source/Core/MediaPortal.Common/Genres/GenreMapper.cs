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

using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MediaPortal.Common.Genres
{
  public class GenreMapper
  {
    private static readonly Dictionary<string, List<GenreMapping>> MUSIC_GENRE_MAP = new Dictionary<string, List<GenreMapping>>();
    private static readonly Dictionary<string, List<GenreMapping>> MOVIE_GENRE_MAP = new Dictionary<string, List<GenreMapping>>();
    private static readonly Dictionary<string, List<GenreMapping>> SERIES_GENRE_MAP = new Dictionary<string, List<GenreMapping>>();
    private static SettingsChangeWatcher<RegionSettings> SETTINGS_CHANGE_WATCHER = null;
    private static string DEFAULT_LANGUAGE = "en-US";
    private static GenreStringManager GENRE_STRINGS = new GenreStringManager();

    static GenreMapper()
    {
      SETTINGS_CHANGE_WATCHER = new SettingsChangeWatcher<RegionSettings>();
      SETTINGS_CHANGE_WATCHER.SettingsChanged += SettingsChanged;
      LoadDefaultLanguage();
    }

    private static void SettingsChanged(object sender, EventArgs e)
    {
      LoadDefaultLanguage();
    }

    private static void LoadDefaultLanguage()
    {
      RegionSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RegionSettings>();
      if (!string.IsNullOrEmpty(settings.Culture))
        DEFAULT_LANGUAGE = settings.Culture;
    }

    private static void InitMusicGenre(string language)
    {
      lock (MUSIC_GENRE_MAP)
      {
        if (MUSIC_GENRE_MAP.ContainsKey(language))
          return;

        MUSIC_GENRE_MAP[language] = new List<GenreMapping>();
        string genreRegex;
        MUSIC_GENRE_MAP[language].AddRange(new GenreMapping[]
        {
          new GenreMapping(MusicGenre.CLASSIC, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Classic", language, out genreRegex) ? genreRegex : @"Classic", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.SOUNDTRACK, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Soundtrack", language, out genreRegex) ? genreRegex : @"Soundtrack", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.NEW_AGE, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "NewAge", language, out genreRegex) ? genreRegex : @"New Age", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.ROCK, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Rock", language, out genreRegex) ? genreRegex : @"Rock", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.METAL, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Metal", language, out genreRegex) ? genreRegex : @"Metal", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.COUNTRY, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Classic", language, out genreRegex) ? genreRegex : @"Country", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.JAZZ, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Jazz", language, out genreRegex) ? genreRegex : @"Jazz", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.RB_SOUL, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Soul", language, out genreRegex) ? genreRegex : @"Soul", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.HIP_HOP_RAP, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Rap", language, out genreRegex) ? genreRegex : @"Rap", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.RAGGAE, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Raggae", language, out genreRegex) ? genreRegex : @"Reggae", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.POP, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Pop", language, out genreRegex) ? genreRegex : @"Pop", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.DANCE, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Dance", language, out genreRegex) ? genreRegex : @"Dance", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.ELECTRONIC, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Electronic", language, out genreRegex) ? genreRegex : @"Electronic", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.COMEDY, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Comedy", language, out genreRegex) ? genreRegex : @"Comedy", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.FOLK, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Folk", language, out genreRegex) ? genreRegex : @"Folk", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.EASY_LISTENING, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "EasyListening", language, out genreRegex) ? genreRegex : @"Easy", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.HOLIDAY, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Holiday", language, out genreRegex) ? genreRegex : @"Holiday", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.WORLD, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "World", language, out genreRegex) ? genreRegex : @"World", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.ALTERNATIVE, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Alternative", language, out genreRegex) ? genreRegex : @"Alternative", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.COMPILATION, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Compilation", language, out genreRegex) ? genreRegex : @"Compilation", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.AUDIOBOOK, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Audiobook", language, out genreRegex) ? genreRegex : @"Audiobook", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.KARAOKE, new Regex(GENRE_STRINGS.TryGetGenreString("Music", "Karaoke", language, out genreRegex) ? genreRegex : @"Karaoke", RegexOptions.IgnoreCase)),
        });
      }
    }

    private static void InitMovieGenre(string language)
    {
      lock (MOVIE_GENRE_MAP)
      {
        if (MOVIE_GENRE_MAP.ContainsKey(language))
          return;

        MOVIE_GENRE_MAP[language] = new List<GenreMapping>();
        string genreRegex;
        MOVIE_GENRE_MAP[language].AddRange(new GenreMapping[]
        {
          new GenreMapping(MovieGenre.ACTION, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Action", language, out genreRegex) ? genreRegex : @"Action", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.ADVENTURE, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Adventure", language, out genreRegex) ? genreRegex : @"Adventure", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.ANIMATION, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Animation", language, out genreRegex) ? genreRegex : @"Animation", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.COMEDY, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Comedy", language, out genreRegex) ? genreRegex : @"Comedy", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.CRIME, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Crime", language, out genreRegex) ? genreRegex : @"Crime", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.DOCUMENTARY, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Documentary", language, out genreRegex) ? genreRegex : @"Documentary", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.DRAMA, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Drama", language, out genreRegex) ? genreRegex : @"Drama", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.FAMILY, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Family", language, out genreRegex) ? genreRegex : @"Family", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.FANTASY, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Fantasy", language, out genreRegex) ? genreRegex : @"Fantasy", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.HISTORY, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "History", language, out genreRegex) ? genreRegex : @"History", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.HORROR, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Horror", language, out genreRegex) ? genreRegex : @"Horror", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.MUSIC, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Music", language, out genreRegex) ? genreRegex : @"Music", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.MYSTERY, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Mystery", language, out genreRegex) ? genreRegex : @"Mystery", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.ROMANCE, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Romance", language, out genreRegex) ? genreRegex : @"Romance", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.SCIENCE_FICTION, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "ScienceFiction", language, out genreRegex) ? genreRegex : @"Science Fiction", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.TV_MOVIE, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "TVMovie", language, out genreRegex) ? genreRegex : @"TV", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.THRILLER, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Thriller", language, out genreRegex) ? genreRegex : @"Thriller", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.WAR, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "War", language, out genreRegex) ? genreRegex : @"War", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.WESTERN, new Regex(GENRE_STRINGS.TryGetGenreString("Movie", "Western", language, out genreRegex) ? genreRegex : @"Western", RegexOptions.IgnoreCase)),
        });
      }
    }

    private static void InitSeriesGenre(string language)
    {
      lock (SERIES_GENRE_MAP)
      {
        if (SERIES_GENRE_MAP.ContainsKey(language))
          return;

        SERIES_GENRE_MAP[language] = new List<GenreMapping>();
        string genreRegex;
        SERIES_GENRE_MAP[language].AddRange(new GenreMapping[]
        {
          new GenreMapping(SeriesGenre.ACTION, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Action", language, out genreRegex) ? genreRegex : @"Action", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.ADVENTURE, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Adventure", language, out genreRegex) ? genreRegex : @"Adventure", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.ANIMATION, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Animation", language, out genreRegex) ? genreRegex : @"Animation|Cartoon|Anime", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.COMEDY, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Comedy", language, out genreRegex) ? genreRegex : @"Comedy", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.CRIME, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Crime", language, out genreRegex) ? genreRegex : @"Crime", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.DOCUMENTARY, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Documentary", language, out genreRegex) ? genreRegex : @"Documentary|Biography", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.DRAMA, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Drama", language, out genreRegex) ? genreRegex : @"Drama", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.FAMILY, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Family", language, out genreRegex) ? genreRegex : @"Family", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.FANTASY, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Fantasy", language, out genreRegex) ? genreRegex : @"Fantasy", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.HISTORY, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "History", language, out genreRegex) ? genreRegex : @"History", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.HORROR, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Horror", language, out genreRegex) ? genreRegex : @"Horror", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.MUSIC, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Music", language, out genreRegex) ? genreRegex : @"Music", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.MYSTERY, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Mystery", language, out genreRegex) ? genreRegex : @"Mystery", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.ROMANCE, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Romance", language, out genreRegex) ? genreRegex : @"Romance", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.SCIENCE_FICTION, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "ScienceFiction", language, out genreRegex) ? genreRegex : @"Science Fiction|Science-Fiction|Sci-Fi", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.THRILLER, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Thriller", language, out genreRegex) ? genreRegex : @"Thriller|Disaster|Suspense", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.WAR, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "War", language, out genreRegex) ? genreRegex : @"War", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.WESTERN, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Western", language, out genreRegex) ? genreRegex : @"Western", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.KIDS, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Kids", language, out genreRegex) ? genreRegex : @"Kids|Children|Teen", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.NEWS, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "News", language, out genreRegex) ? genreRegex : @"News", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.REALITY, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Reality", language, out genreRegex) ? genreRegex : @"Reality", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.SOAP, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Soap", language, out genreRegex) ? genreRegex : @"Soap", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.TALK, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Talk", language, out genreRegex) ? genreRegex : @"Talk", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.POLITICS, new Regex(GENRE_STRINGS.TryGetGenreString("Series", "Politics", language, out genreRegex) ? genreRegex : @"Politic", RegexOptions.IgnoreCase)),
        });
      }
    }

    private static bool AssignMissingGenreIds(List<GenreInfo> genres, List<GenreMapping> genreMap)
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
        if (genreMap != null)
        {
          foreach (GenreMapping map in genreMap)
          {
            if (map.GenrePattern.IsMatch(genre.Name))
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
        }
        if (!genres.Contains(testGenre))
          genres.Add(testGenre);
      }
      return retVal;
    }

    public static bool AssignMissingMusicGenreIds(List<GenreInfo> genres, string language = null)
    {
      if (string.IsNullOrEmpty(language))
        language = DEFAULT_LANGUAGE;
      language = language.ToLowerInvariant();

      if (!MUSIC_GENRE_MAP.ContainsKey(language))
        InitMusicGenre(language);

      return AssignMissingGenreIds(genres, MUSIC_GENRE_MAP[language]);
    }

    public static bool AssignMissingMovieGenreIds(List<GenreInfo> genres, string language = null)
    {
      if (string.IsNullOrEmpty(language))
        language = DEFAULT_LANGUAGE;
      language = language.ToLowerInvariant();

      if (!MOVIE_GENRE_MAP.ContainsKey(language))
        InitMovieGenre(language);

      return AssignMissingGenreIds(genres, MOVIE_GENRE_MAP[language]);
    }

    public static bool AssignMissingSeriesGenreIds(List<GenreInfo> genres, string language = null)
    {
      if (string.IsNullOrEmpty(language))
        language = DEFAULT_LANGUAGE;
      language = language.ToLowerInvariant();

      if (!SERIES_GENRE_MAP.ContainsKey(language))
        InitSeriesGenre(language);

      return AssignMissingGenreIds(genres, SERIES_GENRE_MAP[language]);
    }

    public static ILogger Logger
    {
      get
      {
        return ServiceRegistration.Get<ILogger>();
      }
    }
  }
}
