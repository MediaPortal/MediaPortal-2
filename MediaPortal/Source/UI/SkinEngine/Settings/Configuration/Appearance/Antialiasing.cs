#region Copyright (C) 2007-2011 Team MediaPortal

/*
 *  Copyright (C) 2007-2011 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal 2
 *
 *  MediaPortal 2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal 2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.UI.SkinEngine.DirectX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Appearance
{
  public class Antialiasing : SingleSelectionList
  {
    #region Variables

    private IList<MultisampleType> _multiSampleTypes;

    #endregion

    #region Base overrides

    public override void Load()
    {
      _multiSampleTypes = new List<MultisampleType>(GraphicsDevice.WindowedMultisampleTypes);
      MultisampleType selectedMsType = SettingsManager.Load<AppSettings>().MultisampleType;
      for (int i = 0; i < _multiSampleTypes.Count; i++)
        if (selectedMsType == _multiSampleTypes[i])
          Selected = i;

      // Fill items
      _items = _multiSampleTypes.Select(mst => LocalizationHelper.CreateStaticString(mst.ToString())).ToList();
    }

    public override void Save()
    {
      AppSettings settings = SettingsManager.Load<AppSettings>();
      settings.MultisampleType = _multiSampleTypes[Selected];
      SettingsManager.Save(settings);
      // TODO: Reload DX settings, reset DX device
    }

    #endregion
  }
}
