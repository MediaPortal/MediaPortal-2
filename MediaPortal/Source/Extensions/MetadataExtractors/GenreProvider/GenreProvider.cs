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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.GenreProvider
{
  /// <summary>
  /// GenreProvider converts genres using language files.
  /// </summary>
  public class GenreProvider : IGenreProvider
  {
    private readonly ConcurrentDictionary<string, List<GenreMapping>> MusicGenreMap = new ConcurrentDictionary<string, List<GenreMapping>>();
    private readonly ConcurrentDictionary<string, List<GenreMapping>> MovieGenreMap = new ConcurrentDictionary<string, List<GenreMapping>>();
    private readonly ConcurrentDictionary<string, List<GenreMapping>> SeriesGenreMap = new ConcurrentDictionary<string, List<GenreMapping>>();
    private SettingsChangeWatcher<RegionSettings> SettingChangeWatcher = null;
    private string DEFAULT_LANGUAGE = "en";
    private GenreStringManager GenreStringManager = new GenreStringManager();

    public GenreProvider()
    {
      SettingChangeWatcher = new SettingsChangeWatcher<RegionSettings>();
      SettingChangeWatcher.SettingsChanged += SettingsChanged;
      LoadDefaultLanguage();
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadDefaultLanguage();
    }

    private void LoadDefaultLanguage()
    {
      RegionSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RegionSettings>();
      if (!string.IsNullOrEmpty(settings.Culture))
        DEFAULT_LANGUAGE = settings.Culture;
    }

    private void InitMusicGenre(string language)
    {
      if (MusicGenreMap.ContainsKey(language))
        return;

      var list = new List<GenreMapping>();

      var settings = ServiceRegistration.Get<ISettingsManager>().Load<GenreSettings>();
      if (settings.MusicGenreMappings?.Length > 0)
        list.AddRange(settings.MusicGenreMappings);

      string genreRegex;
      list.AddRange(new GenreMapping[]
      {
        new GenreMapping((int)MusicGenre.Classic, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Classic", language, out genreRegex) ? genreRegex : @"Classic", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Soundtrack, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Soundtrack", language, out genreRegex) ? genreRegex : @"Soundtrack", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.NewAge, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.NewAge", language, out genreRegex) ? genreRegex : @"New Age", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Rock, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Rock", language, out genreRegex) ? genreRegex : @"Rock", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Metal, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Metal", language, out genreRegex) ? genreRegex : @"Metal", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Country, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Classic", language, out genreRegex) ? genreRegex : @"Country", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Jazz, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Jazz", language, out genreRegex) ? genreRegex : @"Jazz", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Soul, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.RbSoul", language, out genreRegex) ? genreRegex : @"Soul", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Rap, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.HipHopRap", language, out genreRegex) ? genreRegex : @"Rap", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Raggae, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Raggae", language, out genreRegex) ? genreRegex : @"Reggae", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Pop, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Pop", language, out genreRegex) ? genreRegex : @"Pop", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Dance, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Dance", language, out genreRegex) ? genreRegex : @"Dance", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Electronic, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Electronic", language, out genreRegex) ? genreRegex : @"Electronic", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Comedy, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Comedy", language, out genreRegex) ? genreRegex : @"Comedy", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Folk, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Folk", language, out genreRegex) ? genreRegex : @"Folk", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.EasyListening, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.EasyListening", language, out genreRegex) ? genreRegex : @"Easy", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Holiday, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Holiday", language, out genreRegex) ? genreRegex : @"Holiday", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.World, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.World", language, out genreRegex) ? genreRegex : @"World", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Alternative, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Alternative", language, out genreRegex) ? genreRegex : @"Alternative", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Compilation, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Compilation", language, out genreRegex) ? genreRegex : @"Compilation", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Audiobook, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Audiobook", language, out genreRegex) ? genreRegex : @"Audiobook", RegexOptions.IgnoreCase)),
        new GenreMapping((int)MusicGenre.Karaoke, new Regex(GenreStringManager.TryGetGenreString("Match", "Music.Karaoke", language, out genreRegex) ? genreRegex : @"Karaoke", RegexOptions.IgnoreCase)),
      });

      MusicGenreMap.TryAdd(language, list);
    }

    private void InitMovieGenre(string language)
    {
      if (MovieGenreMap.ContainsKey(language))
        return;

      var list = new List<GenreMapping>();

      var settings = ServiceRegistration.Get<ISettingsManager>().Load<GenreSettings>();
      if (settings.MovieGenreMappings?.Length > 0)
        list.AddRange(settings.MovieGenreMappings);

      string genreRegex;
      list.AddRange(new GenreMapping[]
      {
          new GenreMapping((int)VideoGenre.Action, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Action", language, out genreRegex) ? genreRegex : @"Action", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Adventure, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Adventure", language, out genreRegex) ? genreRegex : @"Adventure", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Animation, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Animation", language, out genreRegex) ? genreRegex : @"Animation", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Comedy, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Comedy", language, out genreRegex) ? genreRegex : @"Comedy", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Crime, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Crime", language, out genreRegex) ? genreRegex : @"Crime", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Documentary, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Documentary", language, out genreRegex) ? genreRegex : @"Documentary", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Biography, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Biography", language, out genreRegex) ? genreRegex : @"Biography", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Drama, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Drama", language, out genreRegex) ? genreRegex : @"Drama", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Family, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Family", language, out genreRegex) ? genreRegex : @"Family", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Fantasy, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Fantasy", language, out genreRegex) ? genreRegex : @"Fantasy", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.History, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.History", language, out genreRegex) ? genreRegex : @"History", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Horror, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Horror", language, out genreRegex) ? genreRegex : @"Horror", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Music, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Music", language, out genreRegex) ? genreRegex : @"Music", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Mystery, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Mystery", language, out genreRegex) ? genreRegex : @"Mystery", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Romance, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Romance", language, out genreRegex) ? genreRegex : @"Romance", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.ScienceFiction, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.ScienceFiction", language, out genreRegex) ? genreRegex : @"Science Fiction", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.TvMovie, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.TVMovie", language, out genreRegex) ? genreRegex : @"TV", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Thriller, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Thriller", language, out genreRegex) ? genreRegex : @"Thriller", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.War, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.War", language, out genreRegex) ? genreRegex : @"War", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Western, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Western", language, out genreRegex) ? genreRegex : @"Western", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Noir, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Noir", language, out genreRegex) ? genreRegex : @"Noir", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Sport, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Sport", language, out genreRegex) ? genreRegex : @"Sport", RegexOptions.IgnoreCase)),
          new GenreMapping((int)VideoGenre.Superhero, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Superhero", language, out genreRegex) ? genreRegex : @"Superhero", RegexOptions.IgnoreCase)),
      });

      MovieGenreMap.TryAdd(language, list);
    }

    private void InitSeriesGenre(string language)
    {
      if (SeriesGenreMap.ContainsKey(language))
        return;

      var list = new List<GenreMapping>();

      var settings = ServiceRegistration.Get<ISettingsManager>().Load<GenreSettings>();
      if (settings.SeriesGenreMappings?.Length > 0)
        list.AddRange(settings.SeriesGenreMappings);

      string genreRegex;
      list.AddRange(new GenreMapping[]
      {
        new GenreMapping((int)VideoGenre.Action, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Action", language, out genreRegex) ? genreRegex : @"Action", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Adventure, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Adventure", language, out genreRegex) ? genreRegex : @"Adventure", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Animation, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Animation", language, out genreRegex) ? genreRegex : @"Animation|Cartoon|Anime", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Comedy, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Comedy", language, out genreRegex) ? genreRegex : @"Comedy", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Crime, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Crime", language, out genreRegex) ? genreRegex : @"Crime", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Documentary, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Documentary", language, out genreRegex) ? genreRegex : @"Documentary", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Biography, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Biography", language, out genreRegex) ? genreRegex : @"Biography", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Drama, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Drama", language, out genreRegex) ? genreRegex : @"Drama", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Family, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Family", language, out genreRegex) ? genreRegex : @"Family", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Fantasy, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Fantasy", language, out genreRegex) ? genreRegex : @"Fantasy", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.History, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.History", language, out genreRegex) ? genreRegex : @"History", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Horror, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Horror", language, out genreRegex) ? genreRegex : @"Horror", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Music, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Music", language, out genreRegex) ? genreRegex : @"Music", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Mystery, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Mystery", language, out genreRegex) ? genreRegex : @"Mystery", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Romance, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Romance", language, out genreRegex) ? genreRegex : @"Romance", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.ScienceFiction, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.ScienceFiction", language, out genreRegex) ? genreRegex : @"Science Fiction|Science-Fiction|Sci-Fi", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Thriller, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Thriller", language, out genreRegex) ? genreRegex : @"Thriller|Disaster|Suspense", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.War, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.War", language, out genreRegex) ? genreRegex : @"War", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Western, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Western", language, out genreRegex) ? genreRegex : @"Western", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Kids, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Kids", language, out genreRegex) ? genreRegex : @"Kids|Children|Teen", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.News, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.News", language, out genreRegex) ? genreRegex : @"News", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Reality, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Reality", language, out genreRegex) ? genreRegex : @"Reality", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Soap, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Soap", language, out genreRegex) ? genreRegex : @"Soap", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Talk, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Talk", language, out genreRegex) ? genreRegex : @"Talk", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Politics, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Politics", language, out genreRegex) ? genreRegex : @"Politic", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Sport, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Sport", language, out genreRegex) ? genreRegex : @"Sport", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Superhero, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Superhero", language, out genreRegex) ? genreRegex : @"Superhero", RegexOptions.IgnoreCase)),
        new GenreMapping((int)VideoGenre.Game, new Regex(GenreStringManager.TryGetGenreString("Match", "Video.Game", language, out genreRegex) ? genreRegex : @"Game", RegexOptions.IgnoreCase)),
      });

      SeriesGenreMap.TryAdd(language, list);
    }

    public bool GetGenreId(string genreName, string genreCategory, string genreCulture, out int genreId)
    {
      genreId = 0;
      try
      {
        List<string> langs = new List<string>();
        if (string.IsNullOrEmpty(genreCulture))
        {
          langs.AddRange(GenreStringManager.AvailableLanguages.Select(c => c.Name));
        }
        else
        {
          if (genreCulture.Contains("-"))
            genreCulture = new CultureInfo(genreCulture).Parent.Name;
          langs.Add(genreCulture);
        }

        if (string.IsNullOrEmpty(genreName))
          return false;

        foreach (var lang in langs)
        {
          List<GenreMapping> genreMap = null;
          if (GenreCategory.Movie == genreCategory)
          {
            InitMovieGenre(lang);
            if (!MovieGenreMap.TryGetValue(lang, out genreMap))
              continue;
          }
          else if (GenreCategory.Series == genreCategory)
          {
            InitSeriesGenre(lang);
            if (!SeriesGenreMap.TryGetValue(lang, out genreMap))
              continue;
          }
          else if (GenreCategory.Music == genreCategory)
          {
            InitMusicGenre(lang);
            if (!MusicGenreMap.TryGetValue(lang, out genreMap))
              continue;
          }
          else
          {
            return false;
          }

          GenreMapping map = genreMap?.FirstOrDefault(g => g.GenrePattern.IsMatch(genreName));
          if (map != null)
          {
            genreId = map.GenreId;
            return true;
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("GenreProvider: Error getting genre id {0}", ex, genreName);
        return false;
      }
    }

    public bool GetGenreName(int genreId, string genreCategory, string genreCulture, out string genreName)
    {
      genreName = null;
      try
      {
        string labelName = null;
        if (GenreCategory.Movie == genreCategory || GenreCategory.Series == genreCategory)
        {
          VideoGenre genre = (VideoGenre)genreId;
          labelName = $"Video.{genre.ToString()}";
        }
        else if (GenreCategory.Music == genreCategory)
        {
          MusicGenre genre = (MusicGenre)genreId;
          labelName = $"Music.{genre.ToString()}";
        }
        else
        {
          return false;
        }

        if (string.IsNullOrEmpty(genreCulture))
        {
          genreCulture = DEFAULT_LANGUAGE;
        }
        else if (genreCulture.Contains("-"))
        {
          genreCulture = new CultureInfo(genreCulture).Parent.Name;
        }
        return GenreStringManager.TryGetGenreString("Label", labelName, genreCulture, out genreName);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("GenreProvider: Error getting genre name {0}", ex, genreName);
        return false;
      }
    }
  }
}
