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
using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Common;
using MediaPortal.Common.Configuration;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.Plugins.SlimTv.Interfaces.Settings;

namespace MediaPortal.Plugins.SlimTv.Client.Settings.Configuration
{
  public abstract class EpgGenreColorSettingBase : SingleSelectionColoredList
  {
    public const string RES_COLOR_NONE = "[SlimTvClient.NoColor]";
    protected EpgGenre _genre = EpgGenre.Unknown;
    protected readonly List<string> _epgColors = new List<string>
      {
        "#7532A8",
        "#4F7A32",
        "#4E93D2",
        "#ED7D31",
        "#7C7C7C",
        "#C03636",
        "#00817E",
        "#C89800",
        "#481F67",
        "#324d1f",
        "#2B4D89",
        "#B1510F",
        "#404040",
        "#7B2323",
        "#004442",
        "#8E6C00",
        "#000000",
      };

    public EpgGenreColorSettingBase(EpgGenre genre)
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
      foreach (var color in _epgColors)
      {
        if (!string.IsNullOrEmpty(genreColor) && genreColor.ToUpperInvariant() == color.ToUpperInvariant())
          selected = _items.Count;
        _items.Add(new ColoredSelectionItem(ColorTranslator.FromHtml(color), LocalizationHelper.CreateStaticString("")));
      }
      _items.Add(new ColoredSelectionItem(Color.Empty, LocalizationHelper.CreateResourceString(RES_COLOR_NONE)));
      Selected = selected;
    }

    public override void Save()
    {
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>(false);
      if (serverSettings == null)
        return;

      SlimTvGenreColorSettings settings = serverSettings.Load<SlimTvGenreColorSettings>();
      string genreColor = null;
      string colorName = _items[Selected].ResourceString.Evaluate();
      string none = LocalizationHelper.Translate(RES_COLOR_NONE);
      if(none != colorName)
      {
        genreColor = ColorTranslator.ToHtml(_items[Selected].BackgroundColor);
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

  public class EpgMovieGenreColorSetting : EpgGenreColorSettingBase
  {
    public EpgMovieGenreColorSetting() : base(EpgGenre.Movie)
    {}
  }

  public class EpgSeriesGenreColorSetting : EpgGenreColorSettingBase
  {
    public EpgSeriesGenreColorSetting() : base(EpgGenre.Series)
    { }
  }

  public class EpgDocumentaryGenreColorSetting : EpgGenreColorSettingBase
  {
    public EpgDocumentaryGenreColorSetting() : base(EpgGenre.Documentary)
    { }
  }

  public class EpgMusicGenreColorSetting : EpgGenreColorSettingBase
  {
    public EpgMusicGenreColorSetting() : base(EpgGenre.Music)
    { }
  }

  public class EpgKidsGenreColorSetting : EpgGenreColorSettingBase
  {
    public EpgKidsGenreColorSetting() : base(EpgGenre.Kids)
    { }
  }

  public class EpgNewsGenreColorSetting : EpgGenreColorSettingBase
  {
    public EpgNewsGenreColorSetting() : base(EpgGenre.News)
    { }
  }

  public class EpgSportGenreColorSetting : EpgGenreColorSettingBase
  {
    public EpgSportGenreColorSetting() : base(EpgGenre.Sport)
    { }
  }

  public class EpgSpecialGenreColorSetting : EpgGenreColorSettingBase
  {
    public EpgSpecialGenreColorSetting() : base(EpgGenre.Special)
    { }
  }
}
