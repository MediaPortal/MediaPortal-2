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
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.Workflow;

namespace MediaPortal.Services.InputManager
{
  // FIXME Albert: Has to be replaced by different independent implementation for different situations,
  // stacked upon each other. One mapper for the default keys, one for DVD playback, one for GUI dialogs
  // with special shortcuts like default buttons & esc, ...
  // Currently, all the code which accesses players is commented out
  public class InputMapper : IInputMapper
  {
    public Key MapSpecialKey(Keys keycode, bool alt)
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      IPlayerManager players = ServiceScope.Get<IPlayerManager>();
      switch (keycode)
      {
        case Keys.F9:
          ///show context menu
          return Key.ContextMenu;

        case Keys.Up:
          //if (players.Count != 0)
          //  if (players[0].InDvdMenu && !players[0].Paused)
          //    return Key.DvdUp;
          return Key.Up;

        case Keys.Down:
          //if (players.Count != 0)
          //  if (players[0].InDvdMenu && !players[0].Paused)
          //    return Key.DvdDown;
          return Key.Down;

        case Keys.Left:
          //if (players.Count != 0)
          //  if (players[0].InDvdMenu && !players[0].Paused)
          //    return Key.DvdLeft;
          return Key.Left;

        case Keys.Right:
          //if (players.Count != 0)
          //  if (players[0].InDvdMenu && !players[0].Paused)
          //    return Key.DvdRight;
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
            IScreenControl sc = ServiceScope.Get<IScreenControl>();
            if (sc.IsFullScreen)
              sc.SwitchMode(ScreenMode.NormalWindowed, FPS.None);
            else
              sc.SwitchMode(ScreenMode.FullScreenWindowed, FPS.None);
          }
          else
          {
            //if (players.Count != 0)
            //  if (players[0].InDvdMenu && !players[0].Paused)
            //    return Key.DvdSelect;
            return Key.Enter;
          }
          break;
        case Keys.Back:
          {
            // Switch to previous workflow state
            if (inputManager.NeedRawKeyData)
              return Key.BackSpace;
            if (screenManager.IsDialogVisible)
              screenManager.CloseDialog();
            else
              workflowManager.NavigatePop(1);
          }
          break;
        case Keys.Escape:
          if (screenManager.IsDialogVisible)
            screenManager.CloseDialog();
          break;
        case Keys.Space:
          //pause/continue playback
          if (inputManager.NeedRawKeyData)
            return Key.Space;
          //if (players.Count != 0)
          //{
          //  players[0].Paused = !players[0].Paused;
          //  return Key.None;
          //}
          return Key.Space;
        case Keys.M:
          //show dvd menu
          if (inputManager.NeedRawKeyData)
            return Key.None;
          //if (players.Count != 0)
          //  return Key.DvdMenu;
          break;
        case Keys.B:
          if (inputManager.NeedRawKeyData)
            return Key.None;
          //if (players.Count != 0)
          //  //stop playback
          //  players.Dispose();
          break;
        case Keys.S:
          //change zoom mode
          if (inputManager.NeedRawKeyData)
            return Key.None;
          //if (players.Count != 0)
          //  return Key.ZoomMode;
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
