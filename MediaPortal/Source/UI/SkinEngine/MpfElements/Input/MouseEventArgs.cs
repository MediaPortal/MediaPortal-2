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
using MediaPortal.Common;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.MpfElements.Input
{
  /// <summary>
  /// Provides data for mouse specific events that are not mouse button or wheel related.
  /// </summary>
  public class MouseEventArgs : InputEventArgs
  {
    /// <summary>
    /// Creates a new instance of <see cref="MouseEventArgs"/>.
    /// </summary>
    /// <param name="timestamp">Time when the input occurred.</param>
    public MouseEventArgs( /*MouseDevice mouse,*/ int timestamp)
      : base( /*mouse,*/ timestamp)
    { }

    #region public properties

    /// <summary>
    /// Gets the current state of the left mouse button.
    /// </summary>
    public MouseButtonState LeftButton
    {
      get 
      { 
        return System.Windows.Forms.Control.MouseButtons.HasFlag(System.Windows.Forms.MouseButtons.Left) ? 
          MouseButtonState.Pressed : 
          MouseButtonState.Released; 
      }
    }

    /// <summary>
    /// Gets the current state of the right mouse button.
    /// </summary>
    public MouseButtonState RightButton
    {
      get
      {
        return System.Windows.Forms.Control.MouseButtons.HasFlag(System.Windows.Forms.MouseButtons.Right) ?
          MouseButtonState.Pressed :
          MouseButtonState.Released;
      }
    }

    /// <summary>
    /// Gets the current state of the middle mouse button.
    /// </summary>
    public MouseButtonState MiddleButton
    {
      get
      {
        return System.Windows.Forms.Control.MouseButtons.HasFlag(System.Windows.Forms.MouseButtons.Middle) ?
          MouseButtonState.Pressed :
          MouseButtonState.Released;
      }
    }

    /// <summary>
    /// Gets the current state of the 1st extended mouse button.
    /// </summary>
    public MouseButtonState XButton1
    {
      get
      {
        return System.Windows.Forms.Control.MouseButtons.HasFlag(System.Windows.Forms.MouseButtons.XButton1) ?
          MouseButtonState.Pressed :
          MouseButtonState.Released;
      }
    }

    /// <summary>
    /// Gets the current state of the 2nd extended mouse button.
    /// </summary>
    public MouseButtonState XButton2
    {
      get
      {
        return System.Windows.Forms.Control.MouseButtons.HasFlag(System.Windows.Forms.MouseButtons.XButton2) ?
          MouseButtonState.Pressed :
          MouseButtonState.Released;
      }
    }

    #endregion

    #region public methods

    // WPF signature: public Point GetPosition(IInputElement relativeTo)
    //TODO: check if IInputElement should be introduced
    /// <summary>
    /// Returns the position of the mouse pointer relative to an specified element
    /// </summary>
    /// <param name="relativeTo"></param>
    /// <returns></returns>
    public PointF GetPosition(Visual relativeTo)
    {
      var inputManager = ServiceRegistration.Get<IInputManager>();
      var pt = inputManager.MousePosition;
      var uiElement = relativeTo as UIElement;
      if (uiElement != null)
      {
        return uiElement.TransformScreenPoint(new PointF(pt.X, pt.Y));
      }
      return new PointF(pt.X, pt.Y);
    }

    #endregion

    #region base overrides

    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
    {
      var handler = genericHandler as MouseEventHandler;
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
  /// Represents the method that will handle mouse related events that are not mouse button or wheel related.
  /// </summary>
  /// <param name="sender">Sender of the event</param>
  /// <param name="e">Event arguments for this event.</param>
  public delegate void MouseEventHandler(object sender, MouseEventArgs e);

  /// <summary>
  /// Specifies the possible states of a mouse button.
  /// </summary>
  public enum MouseButtonState
  {
    Pressed,
    Released
  }
}
