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

using System;
using MediaPortal.Presentation.Actions;

namespace MediaPortal.Control.InputManager
{
  /// <summary>
  /// Public interface of the input manager.
  /// </summary>
  public interface IInputManager
  {
    /// <summary>
    /// Gets the last time when the mouse was used.
    /// </summary>
    DateTime LastMouseUsageTime { get; }

    /// <summary>
    /// Gets the last time when an input (keyboard or IR) was made.
    /// </summary>
    DateTime LastInputTime { get; }

    /// <summary>
    /// Called to handle a mouse move event.
    /// </summary>
    /// <param name="x">The x corrdinate.</param>
    /// <param name="y">The y coordinate.</param>
    void MouseMove(float x, float y);

    /// <summary>
    /// Called to handle a key event. The key event can come from a mapped keyboard input or from any other
    /// input service.
    /// </summary>
    /// <param name="key">The key which was pressed or generated.</param>
    void KeyPress(Key key);

    /// <summary>
    /// Adds a global command shortcut.
    /// </summary>
    /// <param name="key">The key which triggers the shortcut.</param>
    /// <param name="action">The action which should be executed.</param>
    void AddShortcut(Key key, ActionDlgt action);

    /// <summary>
    /// Removes a global command shortcut.
    /// </summary>
    /// <param name="key">The key which triggers the shortcut.</param>
    void RemoveShortcut(Key key);
  }
}
