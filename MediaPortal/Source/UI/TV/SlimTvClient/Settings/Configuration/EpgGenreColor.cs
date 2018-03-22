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
using System.Drawing;
using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.Plugins.SlimTv.Interfaces.Settings;

namespace MediaPortal.Plugins.SlimTv.Client.Settings.Configuration
{
  public class EpgGenreColorSetting : SingleSelectionList
  {
    protected EpgGenre _genre = EpgGenre.Unknown;
    protected string _none = "None";

    public EpgGenreColorSetting(EpgGenre genre)
    {
      _genre = genre;
    }

    public override void Load()
    {
      _items.Clear();
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>(false);
      if (serverSettings == null)
        return;

      SlimTvGenreColorSettings settings = serverSettings.Load<SlimTvGenreColorSettings>();
      string genreColor = null;
      switch (_genre)
      {
        case EpgGenre.Movie:
          genreColor = settings.MovieGenreColor;
          break;
        case EpgGenre.Series:
          genreColor = settings.SeriesGenreColor;
          break;
        case EpgGenre.Documentary:
          genreColor = settings.DocumentaryGenreColor;
          break;
        case EpgGenre.Music:
          genreColor = settings.MusicGenreColor;
          break;
        case EpgGenre.Kids:
          genreColor = settings.KidsGenreColor;
          break;
        case EpgGenre.News:
          genreColor = settings.NewsGenreColor;
          break;
        case EpgGenre.Sport:
          genreColor = settings.SportGenreColor;
          break;
        case EpgGenre.Special:
          genreColor = settings.SpecialGenreColor;
          break;
      }

      int selected = 0;
      _items.Add(LocalizationHelper.CreateStaticString(_none));
      foreach (var color in Enum.GetValues(typeof(KnownColor)))
      {
        if (!string.IsNullOrEmpty(genreColor) && ColorTranslator.FromHtml(genreColor).ToArgb() == Color.FromKnownColor((KnownColor)color).ToArgb())
          selected = _items.Count;
        _items.Add(LocalizationHelper.CreateStaticString(color.ToString()));
      }
      Selected = selected;
    }

    public override void Save()
    {
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>(false);
      if (serverSettings == null)
        return;

      SlimTvGenreColorSettings settings = serverSettings.Load<SlimTvGenreColorSettings>();
      string genreColor = null;
      string colorName = _items[Selected].Evaluate();
      if(_none != colorName && Enum.TryParse(colorName, out KnownColor selColor))
      {
        genreColor = ColorTranslator.ToHtml(Color.FromArgb(Color.FromKnownColor(selColor).ToArgb()));
      }
      switch (_genre)
      {
        case EpgGenre.Movie:
          settings.MovieGenreColor = genreColor;
          break;
        case EpgGenre.Series:
          settings.SeriesGenreColor = genreColor;
          break;
        case EpgGenre.Documentary:
          settings.DocumentaryGenreColor = genreColor;
          break;
        case EpgGenre.Music:
          settings.MusicGenreColor = genreColor;
          break;
        case EpgGenre.Kids:
          settings.KidsGenreColor = genreColor;
          break;
        case EpgGenre.News:
          settings.NewsGenreColor = genreColor;
          break;
        case EpgGenre.Sport:
          settings.SportGenreColor = genreColor;
          break;
        case EpgGenre.Special:
          settings.SpecialGenreColor = genreColor;
          break;
      }
      serverSettings.Save(settings);
    }
  }

  public class EpgMovieGenreColorSetting : EpgGenreColorSetting
  {
    public EpgMovieGenreColorSetting() : base(EpgGenre.Movie)
    {}
  }

  public class EpgSeriesGenreColorSetting : EpgGenreColorSetting
  {
    public EpgSeriesGenreColorSetting() : base(EpgGenre.Series)
    { }
  }

  public class EpgDocumentaryGenreColorSetting : EpgGenreColorSetting
  {
    public EpgDocumentaryGenreColorSetting() : base(EpgGenre.Documentary)
    { }
  }

  public class EpgMusicGenreColorSetting : EpgGenreColorSetting
  {
    public EpgMusicGenreColorSetting() : base(EpgGenre.Music)
    { }
  }

  public class EpgKidsGenreColorSetting : EpgGenreColorSetting
  {
    public EpgKidsGenreColorSetting() : base(EpgGenre.Kids)
    { }
  }

  public class EpgNewsGenreColorSetting : EpgGenreColorSetting
  {
    public EpgNewsGenreColorSetting() : base(EpgGenre.News)
    { }
  }

  public class EpgSportGenreColorSetting : EpgGenreColorSetting
  {
    public EpgSportGenreColorSetting() : base(EpgGenre.Sport)
    { }
  }

  public class EpgSpecialGenreColorSetting : EpgGenreColorSetting
  {
    public EpgSpecialGenreColorSetting() : base(EpgGenre.Special)
    { }
  }
}
