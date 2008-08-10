#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Globalization;
using MediaPortal.Core.Settings;

namespace MediaPortal.Presentation.Localisation
{
  public class RegionSettings
  {
    #region Variables

    private string _culture;
    private string _region;
    private string _city;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the culture used for localisation
    /// </summary>
    /// <value>The Culture</value>
    [Setting(SettingScope.User, "")]
    public string Culture
    {
      get { return _culture; }
      set { _culture = value; }
    }

    /// <summary>
    /// Gets or sets the region used for localisation
    /// </summary>
    /// <value>The Region (ISO 2 letters)</value>
    //    [Setting(SettingScope.Global, "")]
    public string Region
    {
      get { return _region; }
      set { _region = value; }
    }

    /// <summary>
    /// Gets or sets the city used for localisation
    /// </summary>
    /// <value>The city name</value>
    [Setting(SettingScope.Global, "")]
    public string City
    {
      get { return _city; }
      set { _city = value; }
    }

    #endregion

    #region static Methods

    public static string LocalCulture()
    {
      return CultureInfo.CurrentCulture.Name;
    }

    public static string LocalRegion()
    {
      return RegionInfo.CurrentRegion.TwoLetterISORegionName;
    }

    #endregion
  }
}
