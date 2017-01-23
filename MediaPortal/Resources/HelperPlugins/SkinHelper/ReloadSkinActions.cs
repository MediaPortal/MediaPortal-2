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

using MediaPortal.Common;
using MediaPortal.Helpers.SkinHelper.General;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.Helpers.SkinHelper
{
  public class ReloadSkinActions
  {
    #region Protected fields

    protected string _skinName = null;
    protected string _themeName = null;

    #endregion

    public void RegisterKeyActions()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.AddKeyBinding(Consts.RELOAD_SCREEN_KEY, new VoidKeyActionDlgt(ReloadScreenAction));
      inputManager.AddKeyBinding(Consts.RELOAD_THEME_KEY, new VoidKeyActionDlgt(ReloadThemeAction));
      inputManager.AddKeyBinding(Consts.SAVE_SKIN_AND_THEME_KEY, new VoidKeyActionDlgt(SaveSkinAndThemeAction));
    }

    public void UnregisterKeyActions()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.RemoveKeyBinding(Consts.RELOAD_SCREEN_KEY);
      inputManager.RemoveKeyBinding(Consts.RELOAD_THEME_KEY);
      inputManager.RemoveKeyBinding(Consts.SAVE_SKIN_AND_THEME_KEY);
    }

    static void ReloadScreenAction()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.Reload();
    }

    void ReloadThemeAction()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.SwitchSkinAndTheme(_skinName, _themeName);
    }

    protected static void FindSkinAndTheme(out Skin skin, out Theme theme)
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      ISkinResourceBundle bundle = screenManager.CurrentSkinResourceBundle;
      theme = bundle as Theme;
      skin = bundle as Skin;
      if (theme != null)
        skin = bundle.InheritedSkinResources as Skin;
    }

    void SaveSkinAndThemeAction()
    {
      Skin skin;
      Theme theme;
      FindSkinAndTheme(out skin, out theme);
      _skinName = skin.Name;
      _themeName = theme.Name;
    }
  }
}
