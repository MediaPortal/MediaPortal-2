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
using System.ComponentModel;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.ServiceMonitor.Utilities
{
  public delegate void WinProcHandler(object snder, uint messageId, uint wparam, uint lparam);

  /// <summary>
  /// Receives window messages of an underlying helper window.
  /// </summary>
  public class WindowMessageSink : IDisposable
  {
    #region Private/protected fields

    /// <summary>
    /// A delegate that processes messages of the hidden native window that receives window messages. 
    /// </summary>
    private WindowProcedureHandler _messageHandler;

    internal string WindowClassId { get; private set; }

    internal IntPtr MessageWindowHandle { get; private set; }

    #endregion

    #region Events

    /// <summary>
    /// Public event for the processing window messages.
    /// </summary>
    public event WinProcHandler OnWinProc;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new message sink that receives messages
    /// </summary>
    public WindowMessageSink()
    {
      CreateMessageWindow();
    }

    /// <summary>
    /// Creates a dummy instance that provides an empty pointer rather than a real window handler.
    /// Used at design time.
    /// </summary>
    internal static WindowMessageSink CreateEmpty()
    {
      return new WindowMessageSink {MessageWindowHandle = IntPtr.Zero};
    }

    #endregion

    #region CreateMessageWindow

    /// <summary>
    /// Creates the helper message window that is used to receive messages from the taskbar icon.
    /// </summary>
    private void CreateMessageWindow()
    {
      // Generate a unique ID for the window
      WindowClassId = "MP2-ServiceeMonitor_" + DateTime.Now.Ticks;

      // Register window message handler
      _messageHandler = OnWindowMessageReceived;

      // Create a simple window class which is reference through the messageHandler delegate
      WindowClass wc;

      wc.style = 0;
      wc.lpfnWndProc = _messageHandler;
      wc.cbClsExtra = 0;
      wc.cbWndExtra = 0;
      wc.hInstance = IntPtr.Zero;
      wc.hIcon = IntPtr.Zero;
      wc.hCursor = IntPtr.Zero;
      wc.hbrBackground = IntPtr.Zero;
      wc.lpszMenuName = "";
      wc.lpszClassName = WindowClassId;

      // Register the window class
      WinApi.RegisterClass(ref wc);

      // Create the message window
      MessageWindowHandle = WindowsAPI.CreateWindowEx(0, WindowClassId, "", 0, 0, 0, 1, 1, 0, 0, 0, 0);

      if (MessageWindowHandle == IntPtr.Zero)
        throw new Win32Exception();
    }

    #endregion

    #region Handle Window Messages

    /// <summary>
    /// Callback method that receives messages from the taskbar area.
    /// </summary>
    private long OnWindowMessageReceived(IntPtr hwnd, uint messageId, uint wparam, uint lparam)
    {
      var handler = OnWinProc;
      if (handler != null)
        handler(this, messageId, wparam, lparam);

      // Pass the message to the default window procedure
      return WinApi.DefWindowProc(hwnd, messageId, wparam, lparam);
    }

    #endregion

    #region Dispose

    /// <summary>
    /// Set to true as soon as <see cref="Dispose"/> has been invoked.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Disposes the object.
    /// </summary>
    public void Dispose()
    {
      if (IsDisposed) return;

      WindowsAPI.DestroyWindow(MessageWindowHandle);
      _messageHandler = null;

      IsDisposed = true;

      // This object will be cleaned up by the Dispose method.
      // Therefore, we call GC.SupressFinalize to take this object off the finalization queue 
      // and prevent finalization code for this object from executing a second time.
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// This destructor will run only if the <see cref="Dispose()"/> method does not get called. This gives this class the
    /// opportunity to finalize.
    /// </summary>
    ~WindowMessageSink()
    {
      Dispose();
    }

    #endregion
  }
}