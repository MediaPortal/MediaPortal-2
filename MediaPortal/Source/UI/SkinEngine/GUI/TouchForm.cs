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
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Control.InputManager;

namespace MediaPortal.UI.SkinEngine.GUI
{
  /// <summary>
  /// Base class for multi-touch aware form.
  /// Receives touch notifications through Windows messages and converts them
  /// to touch events TouchDown, TouchUp and Touchmove.
  /// </summary>
  public class TouchForm : Form
  {
    #region Constructor

    [SecurityPermission(SecurityAction.Demand)]
    public TouchForm()
    {
      // Setup handlers
      Load += OnLoadHandler;
      _touchInputSize = Marshal.SizeOf(new TouchInput());
    }

    #endregion

    #region Protected members

    // Touch event handlers
    protected event EventHandler<TouchDownEvent> TouchDown;   // touch down event handler
    protected event EventHandler<TouchUpEvent> TouchUp;       // touch up event handler
    protected event EventHandler<TouchMoveEvent> TouchMove;   // touch move event handler

    #endregion

    #region Windows imports

    ///////////////////////////////////////////////////////////////////////
    // Private class definitions, structures, attributes and native fn's
    // Touch event window message constants [winuser.h]
    private const int WM_TOUCHMOVE = 0x0240;
    private const int WM_TOUCHDOWN = 0x0241;
    private const int WM_TOUCHUP = 0x0242;

    // Touch API defined structures [winuser.h]
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    // ReSharper disable MemberCanBePrivate.Local
    // ReSharper disable UnusedMember.Local
    [StructLayout(LayoutKind.Sequential)]
    private struct TouchInput
    {
      public int X;
      public int Y;
      public IntPtr Source;
      public int ID;
      public TouchEventFlags Flags;
      public TouchInputMask Mask;
      public int Time;
      public IntPtr ExtraInfo;
      public int ContactX;
      public int ContactY;
    }

    private enum TouchWindowFlag : uint
    {
      None = 0x0,
      FineTouch = 0x1,
      WantPalm = 0x2
    }

    // Touch/multitouch access is done through unmanaged code
    [DllImport("user32")]
    private static extern bool RegisterTouchWindow(IntPtr hWnd, TouchWindowFlag flags);

    [DllImport("user32")]
    private static extern bool GetTouchInputInfo(IntPtr hTouchInput, int cInputs, [In, Out] TouchInput[] pInputs, int cbSize);

    [DllImport("user32")]
    private static extern void CloseTouchInputHandle(IntPtr lParam);

    // ReSharper restore UnusedMember.Local
    // ReSharper restore FieldCanBeMadeReadOnly.Local
    // ReSharper restore MemberCanBePrivate.Local

    #endregion

    #region Fields

    private readonly int _touchInputSize;        // size of TouchInput structure

    #endregion

    #region Private members

    /// <summary>
    /// OnLoad window event handler: Registers the form for multi-touch input. 
    /// </summary>
    /// <param name="sender">object that has sent the event</param>
    /// <param name="e">event arguments</param>
    private void OnLoadHandler(Object sender, EventArgs e)
    {
      const TouchWindowFlag ulFlags = TouchWindowFlag.None;
      if (!RegisterTouchWindow(Handle, ulFlags))
      {
        ServiceRegistration.Get<ILogger>().Error("Could not register window for handling touch events");
      }
    }

    /// <summary>
    /// Window procedure. Receives WM_ messages.
    /// Translates WM_TOUCH window messages to touch events.
    /// Normally, touch events are sufficient for a derived class,
    /// but the window procedure can be overriden, if needed.
    /// </summary>
    /// <param name="m">message</param>
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    protected override void WndProc(ref Message m)
    {
      // Decode and handle WM_TOUCH* message.
      bool handled;
      switch (m.Msg)
      {
        case WM_TOUCHDOWN:
        case WM_TOUCHMOVE:
        case WM_TOUCHUP:
          handled = DecodeTouch(ref m);
          break;
        default:
          handled = false;
          break;
      }

      // Call parent WndProc for default message processing.
      base.WndProc(ref m);

      if (handled)
        m.Result = new IntPtr(1);
    }

    /// <summary>
    /// Extracts lower 16-bit word from an 32-bit int.
    /// </summary>
    /// <param name="number">int</param>
    /// <returns>lower word</returns>
    private static int LoWord(int number)
    {
      return number & 0xffff;
    }

    /// <summary>
    /// Decodes and handles WM_TOUCH* messages.
    /// Unpacks message arguments and invokes appropriate touch events.
    /// </summary>
    /// <param name="m">window message</param>
    /// <returns>flag whether the message has been handled</returns>
    private bool DecodeTouch(ref Message m)
    {
      // More than one touchinput may be associated with a touch message,
      // so an array is needed to get all event information.
      int inputCount = LoWord(m.WParam.ToInt32()); // Number of touch inputs, actual per-contact messages

      TouchInput[] inputs = new TouchInput[inputCount];

      // Unpack message parameters into the array of TouchInput structures, each
      // representing a message for one single contact.
      if (!GetTouchInputInfo(m.LParam, inputCount, inputs, _touchInputSize))
      {
        // Get touch info failed.
        return false;
      }

      // For each contact, dispatch the message to the appropriate message
      // handler.
      // Note that for WM_TOUCHDOWN you can get down & move notifications
      // and for WM_TOUCHUP you can get up & move notifications
      // WM_TOUCHMOVE will only contain move notifications
      // and up & down notifications will never come in the same message
      bool handled = false; // // Flag, is message handled
      for (int i = 0; i < inputCount; i++)
      {
        TouchInput ti = inputs[i];
        // Assign a handler to this message.
        if (ti.Flags.HasFlag(TouchEventFlags.Down) && TouchDown != null)
        {
          TouchDown(this, GetEvent<TouchDownEvent>(ti));
          handled = true;
        }
        else if (ti.Flags.HasFlag(TouchEventFlags.Up) && TouchUp != null)
        {
          TouchUp(this, GetEvent<TouchUpEvent>(ti));
          handled = true;
        }
        else if (ti.Flags.HasFlag(TouchEventFlags.Move) && TouchMove != null)
        {
          TouchMove(this, GetEvent<TouchMoveEvent>(ti));
          handled = true;
        }
      }

      CloseTouchInputHandle(m.LParam);

      return handled;
    }

    /// <summary>
    /// Convert the raw touchinput message into a touchevent.
    /// </summary>
    private TE GetEvent<TE>(TouchInput ti) where TE : TouchEvent, new()
    {
      // TOUCHINFO point coordinates and contact size is in 1/100 of a pixel; convert it to pixels.
      // Also convert screen to client coordinates.
      TE te = new TE { ContactY = (float)ti.ContactY / 100, ContactX = (float)ti.ContactX / 100, Id = ti.ID, Time = ti.Time, Mask = ti.Mask, Flags = ti.Flags };
      Point pt = PointToClient(new Point(ti.X / 100, ti.Y / 100));
      te.LocationX = pt.X;
      te.LocationY = pt.Y;
      return te;
    }

    #endregion
  }
}
