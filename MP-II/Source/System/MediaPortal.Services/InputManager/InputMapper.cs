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
using MediaPortal.Core;
using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screen;

namespace MediaPortal.Services.InputManager
{
  public class InputMapper : IInputMapper
  {
    public Key MapSpecialKey(Keys keycode, bool alt)
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      IPlayerCollection players = ServiceScope.Get<IPlayerCollection>(false);
      switch (keycode)
      {
        case Keys.F9:
          ///show context menu
          return Key.ContextMenu;

        case Keys.Up:
          if (players != null)
            if (players.Count != 0)
              if (players[0].InDvdMenu && !players[0].Paused)
                return Key.DvdUp;
          return Key.Up;

        case Keys.Down:
          if (players != null)
            if (players.Count != 0)
              if (players[0].InDvdMenu && !players[0].Paused)
                return Key.DvdDown;
          return Key.Down;

        case Keys.Left:
          if (players != null)
            if (players.Count != 0)
              if (players[0].InDvdMenu && !players[0].Paused)
                return Key.DvdLeft;
          return Key.Left;

        case Keys.Right:
          if (players != null)
            if (players.Count != 0)
              if (players[0].InDvdMenu && !players[0].Paused)
                return Key.DvdRight;
          return Key.Right;

        case Keys.PageUp:
          return Key.PageUp;

        case Keys.PageDown:
          return Key.PageDown;

        case Keys.Home:
          return Key.Home;

        case Keys.End:
          return Key.End;

        case Keys.Enter:
          if (inputManager.NeedRawKeyData)
            return Key.Enter;
          if (alt)
          {
            //switch to fullscreen
            IScreenControl app = ServiceScope.Get<IScreenControl>();
            bool windowed = !app.IsFullScreen;
            if (windowed)
              app.SwitchMode(ScreenMode.FullScreenWindowed, FPS.None);
            else
              app.SwitchMode(ScreenMode.NormalWindowed, FPS.None);
          }
          else
          {
            if (players != null)
              if (players.Count != 0)
                if (players[0].InDvdMenu && !players[0].Paused)
                  return Key.DvdSelect;
            return Key.Enter;
          }
          break;
        case Keys.Back:
          {
            //show previous window
            if (inputManager.NeedRawKeyData)
              return Key.BackSpace;
            screenManager.ShowPreviousScreen();
          }
          break;
        case Keys.Escape:
          //show previous window
          screenManager.ShowPreviousScreen();
          break;
        case Keys.Space:
          //pause/continue playback
          if (inputManager.NeedRawKeyData)
            return Key.Space;
          if (players != null)
            if (players.Count != 0)
            {
              players[0].Paused = !players[0].Paused;
              return Key.None;
            }
          return Key.Space;
        case Keys.M:
          //show dvd menu
          if (inputManager.NeedRawKeyData)
            return Key.None;
          if (players != null)
            if (players.Count != 0)
              return Key.DvdMenu;
          break;
        case Keys.B:
          if (inputManager.NeedRawKeyData)
            return Key.None;
          if (players != null)
            if (players.Count != 0)
              //stop playback
              players.Dispose();
          break;
        case Keys.S:
          //change zoom mode
          if (inputManager.NeedRawKeyData)
            return Key.None;
          if (players != null)
            if (players.Count != 0)
              return Key.ZoomMode;
          break;
      }
      return Key.None;
    }

    public Key MapAlphaNumericKey(char keyChar)
    {
      if (keyChar >= (char)32)
        return new Key(keyChar);
      return Key.None;
    }
  }
}
