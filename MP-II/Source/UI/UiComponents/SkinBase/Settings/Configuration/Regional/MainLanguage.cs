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

using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;
using MediaPortal.Configuration.ConfigurationClasses;

namespace UiComponents.SkinBase.Settings.Configuration.Regional
{
  public class MainLanguage : SingleSelectionList
  {
    #region Variables

    private IList<CultureInfo> _cultures;

    #endregion

    protected static int CompareByName(CultureInfo culture1, CultureInfo culture2)
    {
      return string.Compare(culture1.DisplayName, culture2.DisplayName);
    }

    #region Base overrides

    public override void Load()
    {
      List<CultureInfo> cultures = new List<CultureInfo>(CultureInfo.GetCultures(CultureTypes.SpecificCultures));
      cultures.Sort(CompareByName);
      _cultures = cultures;
      CultureInfo current = ServiceScope.Get<ILocalization>().CurrentCulture;
      // Fill items
      _items = new List<IResourceString>(_cultures.Count);
      for (int i = 0; i < _cultures.Count; i++)
      {
        CultureInfo ci = _cultures[i];
        _items.Add(LocalizationHelper.CreateStaticString(ci.DisplayName));
        if (ci == current)
          Selected = i;
      }
    }

    public override void Save()
    {
      ServiceScope.Get<ILocalization>().ChangeLanguage(_cultures[Selected]);
    }

    public override void Apply()
    {
      ServiceScope.Get<ILocalization>().ChangeLanguage(_cultures[Selected]);
    }

    #endregion

  }
}