#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.UI.Control.InputManager;

namespace MediaPortal.UI.SkinEngine.MpfElements.Input
{
  public class KeyEventArgs : KeyboardEventArgs
  {
    #region protected fields

    protected readonly Key _key;

    #endregion

    /// <summary>
    /// Creates a new instance of <see cref="KeyboardEventArgs"/>.
    /// </summary>
    /// <param name="timestamp">Time when the input occurred.</param>
    /// <param name="key">Key to associate with this event.</param>
    public KeyEventArgs( /*KeyboardDevice keyboard,*/ int timestamp, Key key)
      : base( /*keyboard,*/ timestamp)
    {
      _key = key;
    }

    #region public properties

    /// <summary>
    /// Gets the key of the event
    /// </summary>
    public Key Key
    {
      get { return _key; }
    }

    #endregion

    #region base overrides

    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
    {
      var handler = genericHandler as KeyEventHandler;
      if (handler != null)
      {
        handler(genericTarget, this);
      }
      else
      {
        base.InvokeEventHandler(genericHandler, genericTarget);
      }
    }

    #endregion
  }

  /// <summary>
  /// Represents the method that will handle key related events.
  /// </summary>
  /// <param name="sender">Sender of the event</param>
  /// <param name="e">Event arguments for this event.</param>
  public delegate void KeyEventHandler(object sender, KeyEventArgs e);
}