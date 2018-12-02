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

using MediaPortal.UI.Control.InputManager;

namespace MediaPortal.UI.Presentation.Actions
{
  public delegate void VoidKeyActionDlgt();
  public delegate bool KeyActionDlgt();

  /// <summary>
  /// Mapping of a <see cref="Key"/> to an <see cref="KeyActionDlgt"/> method. Used to register key bindings.
  /// </summary>
  public class KeyAction
  {
    #region Protected fields

    protected Key _key;
    protected KeyActionDlgt _action;

    #endregion

    #region Ctor

    public KeyAction(Key key, KeyActionDlgt action)
    {
      _key = key;
      _action = action;
    }

    #endregion

    /// <summary>
    /// The key, to that the <see cref="Action"/> is mapped.
    /// </summary>
    public Key Key
    {
      get { return _key; }
    }

    /// <summary>
    /// The action to be executed by this shortcut.
    /// </summary>
    public KeyActionDlgt Action
    {
      get { return _action; }
    }
  }
}