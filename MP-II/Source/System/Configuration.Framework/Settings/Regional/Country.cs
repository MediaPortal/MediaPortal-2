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

using System;
using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Configuration.Settings.Regional;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Configuration.Settings;
using MediaPortal.Presentation.Localization;

namespace Components.Configuration.Settings.Regional
{
  /// <summary>
  /// FIXME: merge with main settings
  /// </summary>
  public class Country : SingleSelectionList
  {
    #region Variables

    private IList<RegionInfo> _regionNames;

    #endregion

    #region Public properties

    public override Type SettingsObjectType
    {
      get { return typeof(LocalizationSettings); }
    }

    #endregion

    #region Public Methods

    public override void Load(object settingsObject)
    {
      LocalizationSettings settings = (LocalizationSettings)settingsObject;
      InitializeRegionNames();
      // Initialize the list
      List<IResourceString>  regionNames = new List<IResourceString>(_regionNames.Count);
      string current = null;
      foreach (RegionInfo region in _regionNames)
      {
        if (region.TwoLetterISORegionName.ToLowerInvariant() == settings.CountryCode)
          current = region.NativeName;
        regionNames.Add(LocalizationHelper.CreateResourceString(region.NativeName));
      }
      regionNames.Sort();
      _items = regionNames;
      // Find the item to select
      if (string.IsNullOrEmpty(current)) // Use the systemsetting as default fallback
        current = RegionInfo.CurrentRegion.NativeName;
      for (int i = 0; i < _items.Count; i++)
      {
        if (current == _items[i].Evaluate())
        {
          Selected = i;
          break;
        }
      }
    }

    public override void Save(object settingsObject)
    {
      string selection = _items[Selected].Evaluate();
      foreach (RegionInfo region in _regionNames)
      {
        if (region.NativeName == selection)
        {
          ((LocalizationSettings) settingsObject).CountryCode = region.TwoLetterISORegionName.ToLowerInvariant();
          break;
        }
      }
    }

    #endregion

    #region Private Methods

    private void InitializeRegionNames()
    {
      List<RegionInfo> regionNames = new List<RegionInfo>(110);
      regionNames.Add(new RegionInfo("ae"));
      regionNames.Add(new RegionInfo("al"));
      regionNames.Add(new RegionInfo("am"));
      regionNames.Add(new RegionInfo("ar"));
      regionNames.Add(new RegionInfo("at"));
      regionNames.Add(new RegionInfo("au"));
      regionNames.Add(new RegionInfo("az"));
      regionNames.Add(new RegionInfo("be"));
      regionNames.Add(new RegionInfo("bg"));
      regionNames.Add(new RegionInfo("bh"));
      regionNames.Add(new RegionInfo("bn"));
      regionNames.Add(new RegionInfo("bo"));
      regionNames.Add(new RegionInfo("br"));
      regionNames.Add(new RegionInfo("by"));
      regionNames.Add(new RegionInfo("bz"));
      regionNames.Add(new RegionInfo("ca"));
      //regionNames.Add(new RegionInfo("cb")); // Caribbean
      regionNames.Add(new RegionInfo("ch"));
      regionNames.Add(new RegionInfo("cl"));
      regionNames.Add(new RegionInfo("cn"));
      regionNames.Add(new RegionInfo("co"));
      regionNames.Add(new RegionInfo("cr"));
      regionNames.Add(new RegionInfo("cz"));
      regionNames.Add(new RegionInfo("de"));
      regionNames.Add(new RegionInfo("dk"));
      regionNames.Add(new RegionInfo("do"));
      regionNames.Add(new RegionInfo("dz"));
      regionNames.Add(new RegionInfo("ec"));
      regionNames.Add(new RegionInfo("ee"));
      regionNames.Add(new RegionInfo("eg"));
      regionNames.Add(new RegionInfo("es"));
      regionNames.Add(new RegionInfo("fi"));
      regionNames.Add(new RegionInfo("fo"));
      regionNames.Add(new RegionInfo("fr"));
      regionNames.Add(new RegionInfo("gb"));
      regionNames.Add(new RegionInfo("ge"));
      regionNames.Add(new RegionInfo("gr"));
      regionNames.Add(new RegionInfo("gt"));
      regionNames.Add(new RegionInfo("hk"));
      regionNames.Add(new RegionInfo("hn"));
      regionNames.Add(new RegionInfo("hr"));
      regionNames.Add(new RegionInfo("hu"));
      regionNames.Add(new RegionInfo("id"));
      regionNames.Add(new RegionInfo("ie"));
      regionNames.Add(new RegionInfo("il"));
      regionNames.Add(new RegionInfo("in"));
      regionNames.Add(new RegionInfo("iq"));
      regionNames.Add(new RegionInfo("ir"));
      regionNames.Add(new RegionInfo("is"));
      regionNames.Add(new RegionInfo("it"));
      regionNames.Add(new RegionInfo("jm"));
      regionNames.Add(new RegionInfo("jo"));
      regionNames.Add(new RegionInfo("jp"));
      regionNames.Add(new RegionInfo("ke"));
      regionNames.Add(new RegionInfo("kg"));
      regionNames.Add(new RegionInfo("kr"));
      regionNames.Add(new RegionInfo("kw"));
      regionNames.Add(new RegionInfo("kz"));
      regionNames.Add(new RegionInfo("lb"));
      regionNames.Add(new RegionInfo("li"));
      regionNames.Add(new RegionInfo("lt"));
      regionNames.Add(new RegionInfo("lu"));
      regionNames.Add(new RegionInfo("lv"));
      regionNames.Add(new RegionInfo("ly"));
      regionNames.Add(new RegionInfo("ma"));
      regionNames.Add(new RegionInfo("mc"));
      regionNames.Add(new RegionInfo("mk"));
      regionNames.Add(new RegionInfo("mn"));
      regionNames.Add(new RegionInfo("mo"));
      regionNames.Add(new RegionInfo("mv"));
      regionNames.Add(new RegionInfo("mx"));
      regionNames.Add(new RegionInfo("my"));
      regionNames.Add(new RegionInfo("ni"));
      regionNames.Add(new RegionInfo("nl"));
      regionNames.Add(new RegionInfo("no"));
      regionNames.Add(new RegionInfo("nz"));
      regionNames.Add(new RegionInfo("om"));
      regionNames.Add(new RegionInfo("pa"));
      regionNames.Add(new RegionInfo("pe"));
      regionNames.Add(new RegionInfo("ph"));
      regionNames.Add(new RegionInfo("pk"));
      regionNames.Add(new RegionInfo("pl"));
      regionNames.Add(new RegionInfo("pr"));
      regionNames.Add(new RegionInfo("pt"));
      regionNames.Add(new RegionInfo("py"));
      regionNames.Add(new RegionInfo("qa"));
      regionNames.Add(new RegionInfo("ro"));
      regionNames.Add(new RegionInfo("ru"));
      regionNames.Add(new RegionInfo("sa"));
      regionNames.Add(new RegionInfo("se"));
      regionNames.Add(new RegionInfo("sg"));
      regionNames.Add(new RegionInfo("si"));
      regionNames.Add(new RegionInfo("sk"));
      //regionNames.Add(new RegionInfo("sp")); // Serbia
      regionNames.Add(new RegionInfo("sv"));
      regionNames.Add(new RegionInfo("sy"));
      regionNames.Add(new RegionInfo("th"));
      regionNames.Add(new RegionInfo("tn"));
      regionNames.Add(new RegionInfo("tr"));
      regionNames.Add(new RegionInfo("tt"));
      regionNames.Add(new RegionInfo("tw"));
      regionNames.Add(new RegionInfo("ua"));
      regionNames.Add(new RegionInfo("us"));
      regionNames.Add(new RegionInfo("uy"));
      regionNames.Add(new RegionInfo("uz"));
      regionNames.Add(new RegionInfo("ve"));
      regionNames.Add(new RegionInfo("vn"));
      regionNames.Add(new RegionInfo("ye"));
      regionNames.Add(new RegionInfo("za"));
      regionNames.Add(new RegionInfo("zw"));
      _regionNames = regionNames.ToArray();
    }

    #endregion
  }
}