#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using InputDevices.Common.Mapping;
using MediaPortal.Common;
using MediaPortal.UI.Control.InputManager;
using System;

namespace InputDevices.Mapping.ActionExecutors
{
  /// <summary>
  /// Executes the key press defined by an <see cref="InputAction"/> with type <see cref="InputAction.KEY_ACTION_TYPE"/>.
  /// </summary>
  public class KeyActionExecutor : IInputActionExecutor
  {
    protected Key _key;

    public KeyActionExecutor(InputAction inputAction)
    {
      if (inputAction.Type != InputAction.KEY_ACTION_TYPE)
        throw new ArgumentException($"{nameof(KeyActionExecutor)}: {nameof(InputAction.Type)} must be {InputAction.KEY_ACTION_TYPE}", nameof(inputAction));

      _key = GetKey(inputAction);
    }

    protected static Key GetKey(InputAction inputAction)
    {
      Key key = Key.GetSpecialKeyByName(inputAction.Action);
      // Not a special key, see if its a single printable character
      if (key == null && inputAction.Action.Length == 1)
        key = new Key(inputAction.Action[0]);
      if (key == null)
        throw new ArgumentException($"{nameof(KeyActionExecutor)}: {nameof(InputAction.Action)} '{inputAction.Action}' is not a valid {nameof(Key)}", nameof(inputAction));
      return key;
    }

    public void Execute()
    {
      ServiceRegistration.Get<IInputManager>().KeyPress(_key);
    }
  }
}
