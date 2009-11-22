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

using System.Windows.Forms;
using MediaPortal.UI.Control.InputManager;

namespace MediaPortal.UI.SkinEngine.InputManagement
{
  /// <summary>
  /// Maps keyboard events to <see cref="Key"/> instances.
  /// </summary>
  public static class InputMapper
  {
    public static Key MapSpecialKey(Keys keycode, bool alt)
    {
      switch (keycode)
      {
        case Keys.Cancel:
          return Key.Escape;
        case Keys.Clear:
          return Key.Clear;
        case Keys.Delete:
          return Key.Delete;
        case Keys.Insert:
          return Key.Insert;
        case Keys.Enter:
          if (alt)
            return Key.Fullscreen;
          return Key.Ok;
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
      }
      return Key.None;
    }

    public static Key MapPrintableKeys(char keyChar)
    {
      if (keyChar >= (char) 32)
        return new Key(keyChar);
      return Key.None;
    }
  }
}