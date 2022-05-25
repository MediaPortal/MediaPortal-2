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

using HidInput.Inputs;
using InputDevices.Common.Mapping;
using MediaPortal.UI.Control.InputManager;
using SharpLib.Hid;
using System.Collections.Generic;

namespace HidInput.DefaultMappings
{
  internal class GamePad
  {
    protected static readonly IEnumerable<MappedAction> _defaultMapping = new List<MappedAction>
    {
      new MappedAction(InputAction.CreateKeyAction(Key.Up), new GamePadInput(DirectionPadState.Up)),
      new MappedAction(InputAction.CreateKeyAction(Key.Down), new GamePadInput(DirectionPadState.Down)),
      new MappedAction(InputAction.CreateKeyAction(Key.Left), new GamePadInput(DirectionPadState.Left)),
      new MappedAction(InputAction.CreateKeyAction(Key.Right), new GamePadInput(DirectionPadState.Right)),
      new MappedAction(InputAction.CreateKeyAction(Key.Ok), new GamePadInput(1)),
      new MappedAction(InputAction.CreateKeyAction(Key.Escape), new GamePadInput(2)),
      new MappedAction(InputAction.CreateKeyAction(Key.Info), new GamePadInput(3)),
    }.AsReadOnly();

    public static IEnumerable<MappedAction> DefaultMapping
    {
      get { return _defaultMapping; }
    }
  }
}
