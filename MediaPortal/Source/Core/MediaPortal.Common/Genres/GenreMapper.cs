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
    private static List<GenreMapping> MUSIC_GENRE_MAP = new List<GenreMapping>();
    private static List<GenreMapping> SERIES_GENRE_MAP = new List<GenreMapping>();
    private static List<GenreMapping> MOVIE_GENRE_MAP = new List<GenreMapping>();
    private static SettingsChangeWatcher<GenreSettings> _settingsChangeWatcher = null;

    static GenreMapper()
    {
      //Load settings
      LoadSettings();

      //Save settings
      SaveSettings();

      _settingsChangeWatcher = new SettingsChangeWatcher<GenreSettings>();
      _settingsChangeWatcher.SettingsChanged += SettingsChanged;
    }

    private static void LoadSettings()
    {
      GenreSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<GenreSettings>();

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

    private static void SaveSettings()
    {
      GenreSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<GenreSettings>();
      settings.MusicGenreMappings = MUSIC_GENRE_MAP.ToArray();
      settings.MovieGenreMappings = MOVIE_GENRE_MAP.ToArray();
      settings.SeriesGenreMappings = SERIES_GENRE_MAP.ToArray();

      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    private static void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
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

    public static bool AssignMissingMusicGenreIds(List<GenreInfo> genres)
    {
      return AssignMissingGenreIds(genres, MUSIC_GENRE_MAP);
    }

    public static bool AssignMissingMovieGenreIds(List<GenreInfo> genres)
    {
      return AssignMissingGenreIds(genres, MOVIE_GENRE_MAP);
    }
    
    public static bool AssignMissingSeriesGenreIds(List<GenreInfo> genres)
    {
      return AssignMissingGenreIds(genres, SERIES_GENRE_MAP);
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
