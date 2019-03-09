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

using System.Collections.Generic;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Appearance.Skin
{
  /// <summary>
  /// Custom configuration setting class to change the current skin.
  /// </summary>
  public class SkinConfigSetting : CustomConfigSetting
  {
    #region Protected fields

    protected ICollection<SkinManagement.Skin> _skins = new List<SkinManagement.Skin>();
    protected string _currentSkinName = null;

    #endregion

    public ICollection<SkinManagement.Skin> Skins
    {
      get { return _skins; }
    }

    public string CurrentSkinName
    {
      get { return _currentSkinName; }
      set { _currentSkinName = value; }
    }

    #region Public Methods

    public override void Load()
    {
      _skins.Clear();
      SkinManager skinManager = ServiceRegistration.Get<ISkinResourceManager>() as SkinManager;
      if (skinManager == null)
        return;
      SkinSettings settings = SettingsManager.Load<SkinSettings>();
      _currentSkinName = settings.Skin;
      foreach (SkinManagement.Skin skin in skinManager.Skins.Values)
      {
        if (!skin.IsValid)
          continue;
        _skins.Add(skin);
      }
    }

    public override void Save()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      if (screenManager == null)
        return;
      screenManager.SwitchSkinAndTheme(_currentSkinName, null);
    }

    #endregion
  }
}
