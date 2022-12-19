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
using SharpLib.Win32;
using System;

namespace HidInput.Inputs
{
  public class MouseInput : Input
  {
    const string INPUT_TYPE = "Hid.Mouse";

    public MouseInput(RawInputMouseButtonFlags buttonFlags)
    : base($"{INPUT_TYPE}.{GetMouseButtonId(buttonFlags)}", GetMouseButtonName(buttonFlags), false)
    {

    }

    public static bool TryDecodeEvent(Event hidEvent, InputCollection inputCollection)
    {
      RawInputMouseButtonFlags buttonFlags = hidEvent.RawInput.data.mouse.mouseData.buttonsStr.usButtonFlags;
      switch (buttonFlags)
      {
        case RawInputMouseButtonFlags.RI_MOUSE_LEFT_BUTTON_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_LEFT_BUTTON_UP:
        case RawInputMouseButtonFlags.RI_MOUSE_RIGHT_BUTTON_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_RIGHT_BUTTON_UP:
        case RawInputMouseButtonFlags.RI_MOUSE_WHEEL:
          return false; //Reserve these events for navigation purposes
        case RawInputMouseButtonFlags.RI_MOUSE_MIDDLE_BUTTON_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_4_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_5_DOWN:
          inputCollection.AddInput(new MouseInput(buttonFlags));
          break;
        case RawInputMouseButtonFlags.RI_MOUSE_MIDDLE_BUTTON_UP:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_4_UP:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_5_UP:
          inputCollection.RemoveInput(new MouseInput(buttonFlags));
          break;
        default:
          return false; //Unsupported
      }

      return true;
    }

    protected static string GetMouseButtonId(RawInputMouseButtonFlags buttonFlags)
    {
      switch (buttonFlags)
      {
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_3_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_3_UP:
          return "3";
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_4_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_4_UP:
          return "4";
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_5_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_5_UP:
          return "5";
        default:
          throw new ArgumentException($"{buttonFlags} is not valid, it is possibly reserved for system use", nameof(buttonFlags));
      }
    }

    protected static string GetMouseButtonName(RawInputMouseButtonFlags buttonFlags)
    {
      switch (buttonFlags)
      {
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_3_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_3_UP:
          return "MiddleButton";
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_4_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_4_UP:
          return "Button4";
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_5_DOWN:
        case RawInputMouseButtonFlags.RI_MOUSE_BUTTON_5_UP:
          return "Button5";
        default:
          throw new ArgumentException($"{buttonFlags} is not valid, it is possibly reserved for system use", nameof(buttonFlags));
      }
    }
  }
}
