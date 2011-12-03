#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.Helpers.SkinHelper
{
  public class ReloadSkinActions
  {
    #region Consts

    // F5 is already used for media screen refresh
    public static readonly Key RELOAD_SCREEN_KEY = Key.F3;
    public static readonly Key RELOAD_THEME_KEY = Key.F4;
    public static readonly Key SAVE_SKIN_AND_THEME_KEY = Key.F12;

    #endregion

    #region Protected fields

    protected string _skinName = null;
    protected string _themeName = null;

    #endregion

    public void RegisterKeyActions()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.AddKeyBinding(RELOAD_SCREEN_KEY, ReloadScreenAction);
      inputManager.AddKeyBinding(RELOAD_THEME_KEY, ReloadThemeAction);
      inputManager.AddKeyBinding(SAVE_SKIN_AND_THEME_KEY, SaveSkinAndThemeAction);
    }

    public void UnregisterKeyActions()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.RemoveKeyBinding(RELOAD_SCREEN_KEY);
      inputManager.RemoveKeyBinding(RELOAD_THEME_KEY);
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

    void SaveSkinAndThemeAction()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      _skinName = screenManager.SkinName;
      _themeName = screenManager.ThemeName;
    }
  }
}
