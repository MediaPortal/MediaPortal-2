#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Common.Settings;
using System.Globalization;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class OnlineLibrarySettings
  {
    protected bool _onlyBasicFanArt = false;
    protected bool _useHttps = true;
    protected MatcherSetting[] _musicMatchers = new MatcherSetting[0];
    protected MatcherSetting[] _seriesMatchers = new MatcherSetting[0];
    protected MatcherSetting[] _movieMatchers = new MatcherSetting[0];
    protected GenreMapping[] _musicGenreMap = new GenreMapping[0];
    protected GenreMapping[] _seriesGenreMap = new GenreMapping[0];
    protected GenreMapping[] _movieGenreMap = new GenreMapping[0];
    protected string _musicLanguageCulture = CultureInfo.CurrentUICulture.Name;
    protected string _seriesLanguageCulture = CultureInfo.CurrentUICulture.Name;
    protected string _movieLanguageCulture = CultureInfo.CurrentUICulture.Name;

    //Only download basic FanArt like backdrops, banners, posters and thumbnails
    //Not DiscArt,  ClearArt, Logos etc. 
    [Setting(SettingScope.Global)]
    public bool OnlyBasicFanArt
    {
      get { return _onlyBasicFanArt; }
      set { _onlyBasicFanArt = value; }
    }

    //Use HTTPS when available
    [Setting(SettingScope.Global)]
    public bool UseSecureWebCommunication
    {
      get { return _useHttps; }
      set { _useHttps = value; }
    }

    //Music matcher settings
    [Setting(SettingScope.Global)]
    public MatcherSetting[] MusicMatchers
    {
      get { return _musicMatchers; }
      set { _musicMatchers = value; }
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] MusicGenreMappings
    {
      get { return _musicGenreMap; }
      set { _musicGenreMap = value; }
    }

    [Setting(SettingScope.Global)]
    public string MusicLanguageCulture
    {
      get { return _musicLanguageCulture; }
      set { _musicLanguageCulture = value; }
    }

    //Series matcher settings
    [Setting(SettingScope.Global)]
    public MatcherSetting[] SeriesMatchers
    {
      get { return _seriesMatchers; }
      set { _seriesMatchers = value; }
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] SeriesGenreMappings
    {
      get { return _seriesGenreMap; }
      set { _seriesGenreMap = value; }
    }

    [Setting(SettingScope.Global)]
    public string SeriesLanguageCulture
    {
      get { return _seriesLanguageCulture; }
      set { _seriesLanguageCulture = value; }
    }

    //Movie matcher settings
    [Setting(SettingScope.Global)]
    public MatcherSetting[] MovieMatchers
    {
      get { return _movieMatchers; }
      set { _movieMatchers = value; }
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] MovieGenreMappings
    {
      get { return _movieGenreMap; }
      set { _movieGenreMap = value; }
    }

    [Setting(SettingScope.Global)]
    public string MovieLanguageCulture
    {
      get { return _movieLanguageCulture; }
      set { _movieLanguageCulture = value; }
    }
  }
}
