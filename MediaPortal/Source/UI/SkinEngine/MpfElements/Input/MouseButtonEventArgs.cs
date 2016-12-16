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
using System.Windows.Forms;

namespace MediaPortal.UI.SkinEngine.MpfElements.Input
{
  /// <summary>
  /// Provides data for mouse button related events.
  /// </summary>
  public class MouseButtonEventArgs : MouseEventArgs
  {
    /// <summary>
    /// Creates a new instance of <see cref="MouseEventArgs"/>.
    /// </summary>
    /// <param name="timestamp">Time when the input occurred.</param>
    /// <param name="button">The button associated with this event.</param>
    public MouseButtonEventArgs( /*MouseDevice mouse,*/ int timestamp, MouseButton button)
      : base( /*mouse,*/ timestamp)
    {
      ChangedButton = button;
      ClickCount = 1;
    }

    internal MouseButtonEventArgs( /*MouseDevice mouse,*/ int timestamp, MouseButtons button)
      : base( /*mouse,*/ timestamp)
    {
      switch (button)
      {
        case MouseButtons.Left:
          ChangedButton = MouseButton.Left;
          break;
        case MouseButtons.Right:
          ChangedButton = MouseButton.Right;
          break;
        case MouseButtons.Middle:
          ChangedButton = MouseButton.Middle;
          break;
        case MouseButtons.XButton1:
          ChangedButton = MouseButton.XButton1;
          break;
        case MouseButtons.XButton2:
          ChangedButton = MouseButton.XButton2;
          break;
        default:
          ChangedButton = MouseButton.Left;
          break;
      }
      ClickCount = 1;
    }

    internal MouseButtons WinFormsButton
    {
      get
      {
        switch (ChangedButton)
        {
          case MouseButton.Left:
            return MouseButtons.Left;
          case MouseButton.Right:
            return MouseButtons.Right;
          case MouseButton.Middle:
            return MouseButtons.Middle;
          case MouseButton.XButton1:
            return MouseButtons.XButton1;
          case MouseButton.XButton2:
            return MouseButtons.XButton2;
          default:
            return MouseButtons.Left;
        }
      }
    }

    #region public properties

    /// <summary>
    /// Gets the state of the button
    /// </summary>
    public MouseButtonState ButtonState { get; private set; }

    /// <summary>
    /// Gets the button associated with this event.
    /// </summary>
    public MouseButton ChangedButton { get; private set; }

    /// <summary>
    /// Gets the number of times the button was clicked.
    /// </summary>
    public int ClickCount { get; internal set; }

    #endregion

    #region base overrides

    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
    {
      var handler = genericHandler as MouseButtonEventHandler;
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
  /// Represents the method that will handle mouse button related events.
  /// </summary>
  /// <param name="sender">Sender of the event</param>
  /// <param name="e">Event arguments for this event.</param>
  public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs e);

  /// <summary>
  /// Defines values that specify the buttons on a mouse device.
  /// </summary>
  public enum MouseButton
  {
    Left,
    Right,
    Middle,
    XButton1,
    XButton2
  }
}
