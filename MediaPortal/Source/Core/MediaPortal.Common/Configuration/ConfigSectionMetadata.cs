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

namespace MediaPortal.Common.Configuration
{
  /// <summary>
  /// Section metadata structure. Holds all values to describe a plugin's settings section.
  /// </summary>
  public class ConfigSectionMetadata : ConfigBaseMetadata
  {
    protected string _iconSmallFilePath;
    protected string _iconLargeFilePath;

    public ConfigSectionMetadata(string location, string text, string sort, string iconSmall, string iconLarge, string restrictionGroup)
      : base(location, text, sort, restrictionGroup)
    {
      _iconSmallFilePath = iconSmall;
      _iconLargeFilePath = iconLarge;
    }

    public string IconSmallFilePath
    {
      get { return _iconSmallFilePath; }
    }

    public string IconLargeFilePath
    {
      get { return _iconLargeFilePath; }
    }
  }
}
