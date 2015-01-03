#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Appearance
{
  /// <summary>
  /// Base class for InterpolationMode selections.
  /// </summary>
  public abstract class InterpolationModeSelection : SingleSelectionList
  {
    #region Protected fields

    protected IList<InterpolationMode> _interpolationModes;

    #endregion

    public override void Load()
    {
      // Fill items
      _interpolationModes = Enum.GetValues(typeof(InterpolationMode)).Cast<InterpolationMode>().ToList();
      _items = _interpolationModes.Select(mst => LocalizationHelper.CreateStaticString(mst.ToString())).ToList();

      var appSetting = SettingsManager.Load<AppSettings>();

      Selected = LoadOverride(appSetting);
    }

    public override void Save()
    {
      AppSettings settings = SettingsManager.Load<AppSettings>();
      int selected = Selected;
      var selectedMode = selected > -1 && selected < _interpolationModes.Count ? _interpolationModes[selected] : InterpolationMode.Linear;

      SaveOverride(settings, selectedMode);

      SettingsManager.Save(settings);
    }

    protected abstract int LoadOverride(AppSettings setting);

    protected abstract void SaveOverride(AppSettings settings, InterpolationMode selectedMode);
  }


  /// <summary>
  /// Configuration for <see cref="AppSettings.ImageInterpolationMode"/>.
  /// </summary>
  public class InterpolationModeImage : InterpolationModeSelection
  {
    protected override int LoadOverride(AppSettings setting)
    {
      return _interpolationModes.IndexOf(setting.ImageInterpolationMode);
    }

    protected override void SaveOverride(AppSettings settings, InterpolationMode selectedMode)
    {
      settings.ImageInterpolationMode = selectedMode;
      // Note: changes to this propery will reset DX device
      GraphicsDevice11.Instance.ImageInterpolationMode = selectedMode;
    }
  }

  /// <summary>
  /// Configuration for <see cref="AppSettings.VideoInterpolationMode"/>.
  /// </summary>
  public class InterpolationModeVideo : InterpolationModeSelection
  {
    protected override int LoadOverride(AppSettings setting)
    {
      return _interpolationModes.IndexOf(setting.VideoInterpolationMode);
    }

    protected override void SaveOverride(AppSettings settings, InterpolationMode selectedMode)
    {
      settings.VideoInterpolationMode = selectedMode;
      // Note: changes to this propery will reset DX device
      GraphicsDevice11.Instance.VideoInterpolationMode = selectedMode;
    }
  }
}
