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
using System.Drawing;
using System.Windows.Forms;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Actions;

namespace MediaPortal.UI.Control.InputManager
{
  /// <summary>
  /// Public interface of the input manager.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This interface gives some information about input events and lets you simulate key/mouse events by calling the appropriate methods.
  /// When calling input event methods like <see cref="MouseMove"/> or <see cref="KeyPress"/>, the events are enqueued together with the
  /// "real" input events, i.e. if currently the busy cursor is shown, events may be discarded if necessary.
  /// </para>
  /// <para>
  /// This interface also provides method <see cref="ExecuteCommand"/> to execute commands in the input manager's thread. So, long running
  /// commands can be delegated to the input manager.
  /// </para>
  /// <para>
  /// This class doesn't provide .net events to notify clients of input events by design. All input events are handled by the
  /// system. There are very rare cases where raw input events must really be consumed by other system modules. In those cases,
  /// a special solution must be found.
  /// </para>
  /// This service is multithreading-safe.
  /// </remarks>
  public interface IInputManager
  {
    /// <summary>
    /// Gets the last time when an input (keyboard or IR) was made.
    /// </summary>
    DateTime LastInputTime { get; }

    /// <summary>
    /// Gets the last time when the mouse was used.
    /// </summary>
    DateTime LastMouseUsageTime { get; }

    /// <summary>
    /// Returns the information if the mouse was 
    /// </summary>
    bool IsMouseUsed { get; }

    /// <summary>
    /// Returns the current mouse position;
    /// </summary>
    PointF MousePosition { get; }

    /// <summary>
    /// Called to handle a mouse move event.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    void MouseMove(float x, float y);

    /// <summary>
    /// Called to handle mouse wheel events.
    /// </summary>
    /// <param name="delta">How much the wheel turned.</param>
    void MouseWheel(int delta);

    /// <summary>
    /// Called to handle a mouse click event.
    /// </summary>
    /// <param name="mouseButtons">The buttons which were pressed.</param>
    void MouseClick(MouseButtons mouseButtons);

    /// <summary>
    /// Called to handle a mouse down event.
    /// </summary>
    /// <param name="mouseButtons">The buttons which were pressed.</param>
    /// <param name="clicks">Number of times the mouse button was pressed an released</param>
    void MouseDown(MouseButtons mouseButtons, int clicks);

    /// <summary>
    /// Called to handle a mouse up event.
    /// </summary>
    /// <param name="mouseButtons">The buttons which were pressed.</param>
    /// <param name="clicks">Number of times the mouse button was pressed an released</param>
    void MouseUp(MouseButtons mouseButtons, int clicks);

    /// <summary>
    /// Called to handle a key event. The key event can come from a mapped keyboard input or from any other
    /// input service.
    /// </summary>
    /// <param name="key">The key which was pressed or generated.</param>
    void KeyPress(Key key);

    /// <summary>
    /// Called to handle touch down events. The arguments contain the position where the event was raised.
    /// </summary>
    /// <param name="touchDownEvent">The touch event.</param>
    void TouchDown(TouchDownEvent touchDownEvent);

    /// <summary>
    /// Called to handle touch up events. The arguments contain the position where the event was raised.
    /// </summary>
    /// <param name="touchUpEvent">The touch event.</param>
    void TouchUp(TouchUpEvent touchUpEvent);

    /// <summary>
    /// Called to handle touch move events. The arguments contain the position where the event was raised.
    /// </summary>
    /// <param name="touchMoveEvent">The touch event.</param>
    void TouchMove(TouchMoveEvent touchMoveEvent);

    /// <summary>
    /// Called to execute the given command in the input manager thread.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    void ExecuteCommand(ParameterlessMethod command);

    /// <summary>
    /// Adds a global key binding.
    /// </summary>
    /// <param name="key">The key which triggers the command.</param>
    /// <param name="action">The action which should be executed. If that action returns <c>false</c>,
    /// the input manager will fall back to a default binding, if present.</param>
    void AddKeyBinding(Key key, KeyActionDlgt action);

    /// <summary>
    /// Adds a global key binding.
    /// </summary>
    /// <param name="key">The key which triggers the command.</param>
    /// <param name="action">The action which should be executed.</param>
    void AddKeyBinding(Key key, VoidKeyActionDlgt action);

    /// <summary>
    /// Removes a global key binding.
    /// </summary>
    /// <param name="key">The key which triggers the command.</param>
    void RemoveKeyBinding(Key key);

    /// <summary>
    /// The application has been activated
    /// </summary>
    void ApplicationActivated();

    /// <summary>
    /// The application has been deactivated
    /// </summary>
    void ApplicationDeactivated();
  }
}
