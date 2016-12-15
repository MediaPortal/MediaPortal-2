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
using MediaPortal.Helpers.SkinHelper.Models;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Actions;

namespace MediaPortal.Helpers.SkinHelper
{
  public class LoadSkinThemeActions
  {
    public void RegisterKeyActions()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.AddKeyBinding(Consts.LOAD_SKIN_KEY, new VoidKeyActionDlgt(LoadSkinThemeModel.ShowLoadSkinDialog));
      inputManager.AddKeyBinding(Consts.LOAD_THEME_KEY, new VoidKeyActionDlgt(LoadSkinThemeModel.ShowLoadThemeDialog));
    }

    public void UnregisterKeyActions()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.RemoveKeyBinding(Consts.LOAD_SKIN_KEY);
      inputManager.RemoveKeyBinding(Consts.LOAD_THEME_KEY);
    }
  }
}
