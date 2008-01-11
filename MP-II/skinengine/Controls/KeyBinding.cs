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

using MediaPortal.Core.InputManager;

namespace SkinEngine.Controls
{
  public class KeyBinding : Control
  {
    #region variables

    private string _key;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyBinding"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public KeyBinding(Control parent)
      : base(parent) {}

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    public string Key
    {
      get { return _key; }
      set { _key = value; }
    }

    /// <summary>
    /// handles any keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      if (key == MediaPortal.Core.InputManager.Key.None)
      {
        return;
      }
      if (key.ToString() == _key)
      {
        Execute();
      }
    }
  }
}
