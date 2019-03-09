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

using MediaPortal.Common.Certifications;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.UiComponents.Media.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Settings.Configuration
{
  public class MovieCertificationCountry : SingleSelectionList
  {
    private const string REGION_FORMAT = "{0} ({1})";

    #region Variables

    private IList<RegionInfo> _regions;

    #endregion

    public MovieCertificationCountry()
    {
      List<RegionInfo> regions = CertificationMapper.GetSupportedMovieCertificationCountries().Select(r => new RegionInfo(r)).ToList();
      regions.Sort(CompareByName);
      _regions = regions;
    }

    protected static int CompareByName(RegionInfo region1, RegionInfo region2)
    {
      return string.Compare(region1.DisplayName, region2.DisplayName);
    }

    public override void Load()
    {
      base.Load();

      MediaCertificationSettings settings = SettingsManager.Load<MediaCertificationSettings>();

      int selected = 0;
      _items = new List<IResourceString>(_regions.Count + 1);
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Media.Certification.None]"));
      for (int i = 0; i < _regions.Count; i++)
      {
        RegionInfo ri = _regions[i];
        _items.Add(LocalizationHelper.CreateStaticString(string.Format(REGION_FORMAT, ri.DisplayName, ri.Name)));
        if (ri.Name == settings.DisplayMovieCertificationCountry)
          selected = i + 1;
      }
      Selected = selected;
    }

    public override void Save()
    {
      base.Save();

      MediaCertificationSettings settings = SettingsManager.Load<MediaCertificationSettings>();
      settings.DisplayMovieCertificationCountry = "";
      if (Selected > 0)
      {
        settings.DisplayMovieCertificationCountry = _regions[Selected - 1].Name;
      }
      SettingsManager.Save(settings);
      CertificationHelper.DisplayMovieCertificationCountry = settings.DisplayMovieCertificationCountry;
    }
  }
}
