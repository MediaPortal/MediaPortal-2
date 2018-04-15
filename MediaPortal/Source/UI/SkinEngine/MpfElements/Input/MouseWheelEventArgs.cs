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

namespace MediaPortal.UI.SkinEngine.MpfElements.Input
{
  /// <summary>
  /// Provides data for mouse wheel related events.
  /// </summary>
  public class MouseWheelEventArgs : MouseEventArgs
  {
    /// <summary>
    /// Creates a new instance of <see cref="MouseEventArgs"/>.
    /// </summary>
    /// <param name="timestamp">Time when the input occurred.</param>
    /// <param name="delta">How much the mouse wheel turned.</param>
    public MouseWheelEventArgs( /*MouseDevice mouse,*/ int timestamp, int delta)
      : base( /*mouse,*/ timestamp)
    {
      Delta = delta;
    }

    #region public properties

    /// <summary>
    /// Gets how much the mouse wheel turned.
    /// </summary>
    public int Delta { get; private set; }

    /// <summary>
    /// Gets how many detentes the mouse wheel turned
    /// </summary>
    /// <remarks>
    /// NumDetents = <see cref="Delta"/> / 120
    /// </remarks>
    public int NumDetents
    {
      get { return Delta / 120; }
    }

    #endregion

    #region base overrides

    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
    {
      var handler = genericHandler as MouseWheelEventHandler;
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
  /// Represents the method that will handle mouse wheel related events.
  /// </summary>
  /// <param name="sender">Sender of the event</param>
  /// <param name="e">Event arguments for this event.</param>
  public delegate void MouseWheelEventHandler(object sender, MouseWheelEventArgs e);
}
