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

using InputDevices.Common.Inputs;
using SharpLib.Hid;
using System;

namespace HidInput.Inputs
{
  public class GamePadInput : Input
  {
    const string INPUT_TYPE = "Hid.GamePad";

    public GamePadInput(ushort usage)
    : base($"{INPUT_TYPE}.Button{usage}", $"Button{usage}", false)
    {

    }

    public GamePadInput(DirectionPadState directionPadState)
    : base($"{INPUT_TYPE}.Pad{directionPadState}", $"Pad{directionPadState}", false)
    {
      if (directionPadState == DirectionPadState.Rest)
        throw new ArgumentException($"{directionPadState} is not a valid state", nameof(directionPadState));
    }

    public static bool TryDecodeEvent(Event hidEvent, InputCollection inputCollection)
    {
      if (!hidEvent.IsGeneric || hidEvent.Device?.IsGamePad != true)
        return false;

      inputCollection.RemoveAll<GamePadInput>();

      DirectionPadState directionPadState = GetDirectionPadState(hidEvent);
      if (directionPadState != DirectionPadState.Rest)
        inputCollection.AddInput(new GamePadInput(directionPadState));

      foreach (ushort usage in hidEvent.Usages)
        inputCollection.AddInput(new GamePadInput(usage));

      return true;
    }

    static DirectionPadState GetDirectionPadState(Event hidEvent)
    {
      try
      {
        // This can throw under certain circumstances, not sure why...
        return hidEvent.GetDirectionPadState();
      }
      catch
      {
        return DirectionPadState.Rest;
      }
    }
  }
}
