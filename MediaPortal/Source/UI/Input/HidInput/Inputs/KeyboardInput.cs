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

using HidInput.Windows;
using InputDevices.Common.Inputs;
using SharpLib.Hid;
using System.Windows.Forms;

namespace HidInput.Inputs
{
  public class KeyboardInput : Input
  {
    protected static readonly KeysConverter _keysConverter = new KeysConverter();

    public KeyboardInput(Keys key)
    : base(((int)key).ToString(), _keysConverter.ConvertToString(key), IsModifierKey(key))
    {
      Key = key;
    }

    public Keys Key { get; }

    public static bool TryDecodeEvent(Event hidEvent, InputCollection inputCollection)
    {
      if (!hidEvent.IsKeyboard || hidEvent.VirtualKey == Keys.None)
        return false;
      KeyboardInput keyboardInput = new KeyboardInput(hidEvent.VirtualKey);
      if (hidEvent.IsButtonDown)
        inputCollection.AddInput(keyboardInput);
      else
        inputCollection.RemoveInput(keyboardInput);
      return true;
    }

    public static bool TryDecodeMessage(ref Message message, InputCollection inputCollection)
    {
      int msg = WindowsMessageUtils.NormalizeKeyMessageId(message.Msg);
      if (msg != WindowsMessageUtils.WM_KEYDOWN && msg != WindowsMessageUtils.WM_KEYUP)
        return false;
      Keys virtualKey = (Keys)message.WParam & Keys.KeyCode;
      KeyboardInput keyboardInput = new KeyboardInput(virtualKey);
      if (msg == WindowsMessageUtils.WM_KEYDOWN)
        inputCollection.AddInput(keyboardInput);
      else
        inputCollection.RemoveInput(keyboardInput);
      return true;
    }

    protected static bool IsModifierKey(Keys key)
    {
      switch (key)
      {
        case Keys.ShiftKey:
        case Keys.LShiftKey:
        case Keys.RShiftKey:
        case Keys.ControlKey:
        case Keys.LControlKey:
        case Keys.RControlKey:
        case Keys.Menu:
        case Keys.LMenu:
        case Keys.RMenu:
        case Keys.LWin:
        case Keys.RWin:
          return true;
        default:
          return false;
      }
    }
  }
}
