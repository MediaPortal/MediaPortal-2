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

using System.Collections.Generic;

namespace MediaPortal.Control.InputManager
{
  /// <summary>
  /// interface to the input manager
  /// </summary>
  public delegate void MouseMoveHandler(float x, float y);

  public delegate void KeyPressedHandler(ref Key key);

  public interface IInputManager
  {
    event MouseMoveHandler MouseMoved;
    event KeyPressedHandler KeyPressed;

    /// <summary>
    /// Gets or sets a value indicating whether skinengine needs raw key data (for a textbox for example)
    /// </summary>
    /// <value><c>true</c> if [need raw key data]; otherwise, <c>false</c>.</value>
    bool NeedRawKeyData { get; set; }

    /// <summary>
    /// Called when the mouse moved.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    void MouseMove(float x, float y);

    /// <summary>
    /// Called when a key was pressed.
    /// </summary>
    /// <param name="key">The key.</param>
    void KeyPress(Key key);

    /// <summary>
    /// Called by the skin when it wants to press a key
    /// </summary>
    /// <param name="keyName">string containing the key name.</param>
    void KeyPress(string keyName);

    /// <summary>
    /// Returns all registered keys.
    /// </summary>
    /// <value>The keys.</value>
    ICollection<Key> Keys { get; }
  }
}
