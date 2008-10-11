#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System.Globalization;

using MediaPortal.Core.Settings;

namespace MediaPortal.Configuration.Settings.Regional
{
  /// <summary>
  /// Holds all settings related to localization.
  /// </summary>
  public class LocalizationSettings
  {
    #region Variables

    private string _language;
    private string _continent;
    private string _country;
    private string _region;
    private string _city;

    #endregion

    #region Properties

    [Setting(SettingScope.User, "en")]
    public string LanguageCode
    {
      get { return _language; }
      set { _language = value; }
    }

    [Setting(SettingScope.User, "")]
    public string Continent
    {
      get { return _language; }
      set { _language = value; }
    }

    [Setting(SettingScope.User, "")]
    public string CountryCode
    {
      get
      {
        if (_country == "") // Force the default value
          _country = RegionInfo.CurrentRegion.TwoLetterISORegionName.ToLower(new CultureInfo("en"));
        return _country;
      }
      set { _country = value; }
    }

    [Setting(SettingScope.User, "")]
    public string Region
    {
      get { return _region; }
      set { _region = value; }
    }

    [Setting(SettingScope.User, "")]
    public string City
    {
      get { return _city; }
      set { _city = value; }
    }

    #endregion
  }
}