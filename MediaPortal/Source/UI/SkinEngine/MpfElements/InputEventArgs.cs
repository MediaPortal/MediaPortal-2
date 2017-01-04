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

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  /// <summary>
  /// Provides data for input related events.
  /// </summary>
  public class InputEventArgs : RoutedEventArgs
  {
    /// <summary>
    /// Creates an new instance of <see cref="InputEventArgs"/>
    /// </summary>
    /// <param name="timestamp">Time when the input occurred.</param>
    public InputEventArgs( /*InputDevice inputDevice, */ int timestamp)
    {
      //Device = inputDevice;
      Timestamp = timestamp;
    }

    #region public properties

    //TODO: add InputDevice base class and derivatives
    //public InputDevice Device { get; internal set; }

    /// <summary>
    /// Gets the time when the input occurred.
    /// </summary>
    public int Timestamp { get; private set; }

    #endregion

    #region base overrides

    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
    {
      var handler = genericHandler as InputEventHandler;
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
  /// Represents the method that will handle mouse related events that are not mouse or key related.
  /// </summary>
  /// <param name="sender">Sender of the event</param>
  /// <param name="e">Event arguments for this event.</param>
  public delegate void InputEventHandler(object sender, InputEventArgs e);
}
