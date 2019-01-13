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

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MediaPortal.UI.Control.InputManager;

namespace MediaPortal.UI.SkinEngine.InputManagement
{
  /// <summary>
  /// Maps keyboard events to <see cref="Key"/> instances.
  /// </summary>
  public static class InputMapper
  {
    public static Key MapSpecialKey(KeyEventArgs args)
    {
      return MapSpecialKey(args.KeyCode, args.Alt, args.Shift, args.Control);
    }

    private static class Keyboard
    {
      [Flags]
      private enum KeyStates
      {
        None = 0,
        Down = 1,
        Toggled = 2
      }

      [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
      private static extern short GetKeyState(int keyCode);

      private static KeyStates GetKeyState(Keys key)
      {
        KeyStates state = KeyStates.None;

        short retVal = GetKeyState((int)key);

        // If the high-order bit is 1, the key is down, otherwise, it is up.
        if ((retVal & 0x8000) == 0x8000)
          state |= KeyStates.Down;

        // If the low-order bit is 1, the key is toggled.
        if ((retVal & 1) == 1)
          state |= KeyStates.Toggled;

        return state;
      }

      public static bool IsKeyDown(Keys key)
      {
        return GetKeyState(key).HasFlag(KeyStates.Down);
      }

      public static bool IsKeyToggled(Keys key)
      {
        return GetKeyState(key).HasFlag(KeyStates.Toggled);
      }
    }

    public static Key MapSpecialKey(Keys keycode, bool alt, bool shift, bool control)
    {
      switch (keycode)
      {
        case Keys.Add:
          if (alt)
            return Key.VolumeUp;
          break;
        case Keys.Subtract:
          if (alt)
            return Key.VolumeDown;
          break;
        case Keys.Cancel:
          return Key.Escape;
        case Keys.Clear:
          return Key.Clear;
        case Keys.Delete:
          return Key.Delete;
        case Keys.Insert:
          return Key.Insert;
        case Keys.Enter:
          if (!Keyboard.IsKeyDown(Keys.LWin))
          {
            if (alt)
              return Key.Fullscreen;
            return Key.Ok;
          }
          break;
        case Keys.Back:
          return Key.BackSpace;
        case Keys.Escape:
          return Key.Escape;

        case Keys.MediaNextTrack:
          return Key.Next;
        case Keys.MediaPlayPause:
          return Key.PlayPause;
        case Keys.MediaPreviousTrack:
          return Key.Previous;
        case Keys.MediaStop:
          return Key.Stop;
        case Keys.Pause:
          return Key.Pause;
        case Keys.Play:
          return Key.Play;

        case Keys.VolumeMute:
          return Key.Mute;
        case Keys.VolumeDown:
          return Key.VolumeDown;
        case Keys.VolumeUp:
          return Key.VolumeUp;

        case Keys.Up:
          return Key.Up;
        case Keys.Down:
          return Key.Down;
        case Keys.Left:
          return Key.Left;
        case Keys.Right:
          return Key.Right;

        case Keys.PageUp:
          return Key.PageUp;
        case Keys.PageDown:
          return Key.PageDown;

        case Keys.Home:
          return Key.Home;
        case Keys.End:
          return Key.End;

        case Keys.Apps:
          return Key.ContextMenu;
        case Keys.Zoom:
          return Key.ZoomMode;

        case Keys.Sleep:
          return Key.Power;

        case Keys.F1:
          if (alt || control || shift) break;
          return Key.F1;
        case Keys.F2:
          if (alt || control || shift) break;
          return Key.F2;
        case Keys.F3:
          if (alt || control || shift) break;
          return Key.F3;
        case Keys.F4:
          if (alt)
            return Key.Close;
          return Key.F4;
        case Keys.F5:
          if (alt || control || shift) break;
          return Key.F5;
        case Keys.F6:
          if (alt || control || shift) break;
          return Key.F6;
        case Keys.F7:
          if (alt || control || shift) break;
          return Key.F7;
        case Keys.F8:
          if (alt || control || shift) break;
          return Key.F8;
        case Keys.F9:
          if (alt || control || shift) break;
          return Key.F9;
        case Keys.F10:
          if (alt || control) break;
          if (shift) return Key.ContextMenu;
          return Key.F10;
        case Keys.F11:
          if (alt || control || shift) break;
          return Key.F11;
        case Keys.F12:
          if (alt || control || shift) break;
          return Key.F12;
        case Keys.F13:
          if (alt || control || shift) break;
          return Key.F13;
        case Keys.F14:
          if (alt || control || shift) break;
          return Key.F14;
        case Keys.F15:
          if (alt || control || shift) break;
          return Key.F15;
        case Keys.F16:
          if (alt || control || shift) break;
          return Key.F16;
        case Keys.F17:
          if (alt || control || shift) break;
          return Key.F17;
        case Keys.F18:
          if (alt || control || shift) break;
          return Key.F18;
        case Keys.F19:
          if (alt || control || shift) break;
          return Key.F19;
        case Keys.F20:
          if (alt || control || shift) break;
          return Key.F20;
        case Keys.F21:
          if (alt || control || shift) break;
          return Key.F21;
        case Keys.F22:
          if (alt || control || shift) break;
          return Key.F22;
        case Keys.F23:
          if (alt || control || shift) break;
          return Key.F23;
        case Keys.F24:
          if (alt || control || shift) break;
          return Key.F24;

        case Keys.X:
          if (control)
            return Key.Cut;
          break;
        case Keys.C:
          if (control)
            return Key.Copy;
          break;
        case Keys.V:
          if (control)
            return Key.Paste;
          break;
      }
      return Key.None;
    }

    public static Key MapPrintableKeys(char keyChar)
    {
      if (keyChar >= (char)32)
        return new Key(keyChar);
      return Key.None;
    }
  }
}
