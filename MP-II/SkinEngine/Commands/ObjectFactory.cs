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

using MediaPortal.Core;
using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screen;
using MediaPortal.SkinEngine.Models;

namespace MediaPortal.SkinEngine.Commands
{
  internal class ObjectFactory
  {
    public static object GetObject(string name)
    {
      if (name == "ScreenManager")
      {
        IScreenManager manager = ServiceScope.Get<IScreenManager>();
        return manager;
      }

      if (name == "InputManager")
      {
        IInputManager manager = ServiceScope.Get<IInputManager>();
        return manager;
      }

      if (name == "Players")
      {
        return ServiceScope.Get<IPlayerCollection>();
      }

      Model model = ModelManager.Instance.GetModelByInternalName(name);
      if (model != null)
      {
        return model.Instance;
      }
      return null;
    }
  }
}
