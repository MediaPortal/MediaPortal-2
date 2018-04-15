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
using MediaPortal.Utilities.Xml;

namespace MediaPortal.Plugins.RefreshRateChanger.Settings
{
  public class RefreshRateChangerSettings
  {
    public static SerializableDictionary<int, int> DEFAULT_MAPPINGS = new SerializableDictionary<int, int>
    {
      { 23, 23 }, // CINEMA
      { 24, 24 }, // CINEMA24
      { 25, 50 }, // PAL
      { 50, 50 }, // PALHD
      { 29, 59 }, // NTSC
      { 59, 59 }, // NTSCHD
      { 30, 60 }, // ATSC
      { 60, 60 }  // ATSCHD
    };

    public RefreshRateChangerSettings()
    {
      RateMappings = DEFAULT_MAPPINGS;
    }

    /// <summary>
    /// IsEnabled or disable refresh rate changes
    /// </summary>
    [Setting(SettingScope.User, true)]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Contains a custom mapping of refresh rates. This can be used to override the default mappings.
    /// </summary>
    [Setting(SettingScope.User)]
    public SerializableDictionary<int, int> RateMappings { get; set; }
  }
}
