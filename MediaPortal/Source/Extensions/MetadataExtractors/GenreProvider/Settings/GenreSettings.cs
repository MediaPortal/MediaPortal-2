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

using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.GenreProvider
{
  public class GenreSettings
  {
    protected GenreMapping[] _musicGenreMap = new GenreMapping[0];
    protected GenreMapping[] _seriesGenreMap = new GenreMapping[0];
    protected GenreMapping[] _movieGenreMap = new GenreMapping[0];
    protected GenreMapping[] _epgGenreMap = new GenreMapping[0];

    public GenreSettings()
    {
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] MusicGenreMappings
    {
      get { return _musicGenreMap; }
      set { _musicGenreMap = value; }
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] SeriesGenreMappings
    {
      get { return _seriesGenreMap; }
      set { _seriesGenreMap = value; }
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] MovieGenreMappings
    {
      get { return _movieGenreMap; }
      set { _movieGenreMap = value; }
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] EpgGenreMappings
    {
      get { return _epgGenreMap; }
      set { _epgGenreMap = value; }
    }
  }
}
