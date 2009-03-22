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

using MediaPortal.Control.InputManager;

namespace MediaPortal.Presentation.Actions
{
  public delegate bool ActionDlgt();

  /// <summary>
  /// Mapping of a <see cref="Key"/> to an <see cref="ActionDlgt"/> method. Used to register key bindings.
  /// </summary>
  public class KeyAction
  {
    #region Protected fields

    protected Key _key;
    protected ActionDlgt _action;

    #endregion

    #region Ctor

    public KeyAction(Key key, ActionDlgt action)
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
    public ActionDlgt Action
    {
      get { return _action; }
    }
  }
}