#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Core.Runtime;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core.Logging;
using MediaPortal.UI.General;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;

using MediaPortal.UI.SkinEngine.Settings;
using SlimDX.Direct3D9;
using Screen=MediaPortal.UI.SkinEngine.ScreenManagement.Screen;

namespace MediaPortal.UI.SkinEngine.GUI
{
  public delegate void SwitchModeDelegate(ScreenMode mode);

  public partial class MainForm : Form, IScreenControl
  {
    // TODO: Make this configurable
    protected static TimeSpan SCREENSAVER_TIMEOUT = TimeSpan.FromMinutes(5);

    private Thread _renderThread;
    private bool _renderThreadStopped;
    private Size _previousWindowClientSize;
    private Point _previousWindowLocation;
    private FormWindowState _previousWindowState;
    private Point _previousMousePosition;
    private ScreenMode _mode = ScreenMode.NormalWindowed;
    private bool _hasFocus = false;
    private readonly ScreenManager _screenManager;
    protected bool _isScreenSaverEnabled = true;
    protected bool _isScreenSaverActive = false;
    protected bool _mouseHidden = false;
    private readonly object _reclaimDeviceSyncObj = new object();

    private bool _adaptToSizeEnabled;

    public MainForm(ScreenManager screenManager)
    {
      _adaptToSizeEnabled = false;
      _screenManager = screenManager;

      ServiceRegistration.Get<ILogger>().Debug("Registering DirectX MainForm as IScreenControl service");
      ServiceRegistration.Set<IScreenControl>(this);

      InitializeComponent();
      CheckForIllegalCrossThreadCalls = false;

      AppSettings appSettings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();

      _previousMousePosition = new Point(-1, -1);

      Size desiredWindowedSize = new Size(SkinContext.SkinResources.SkinWidth, SkinContext.SkinResources.SkinHeight);

      _previousWindowLocation = Location;
      _previousWindowClientSize = desiredWindowedSize;
      _previousWindowState = FormWindowState.Normal;

      if (appSettings.FullScreen)
        SwitchToFullscreen();
      else
        SwitchToWindowedSize(Location, desiredWindowedSize, false);

      SkinContext.Form = this;
      SkinContext.WindowSize = ClientSize;

      // GraphicsDevice has to be initialized after the form was sized correctly
      ServiceRegistration.Get<ILogger>().Debug("DirectX MainForm: Initialize DirectX");
      GraphicsDevice.Initialize(this);

      Application.Idle += OnApplicationIdle;
      _adaptToSizeEnabled = true;
    }

    protected void SwitchToFullscreen()
    {
      Location = new Point(0, 0);
      // TODO Albert78: Use the bounds of the screen MP is displayed on
      ClientSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
      FormBorderStyle = FormBorderStyle.None;
      _mode = ScreenMode.FullScreen;
    }

    protected void SwitchToWindowedSize(Point location, Size clientSize, bool maximize)
    {
      WindowState = FormWindowState.Normal;
      FormBorderStyle = FormBorderStyle.Sizable;
      Location = location;
      ClientSize = clientSize;
      // We must restore the window state after having set the ClientSize/Location to make the window remember the
      // non-maximized bounds
      WindowState = maximize ? FormWindowState.Maximized : FormWindowState.Normal;
      _mode = ScreenMode.NormalWindowed;
    }

    public void DisposeDirectX()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      logger.Debug("DirectX MainForm: Dispose DirectX");
      GraphicsDevice.Dispose();
    }

    protected void StoreClientBounds()
    {
      if (_mode == ScreenMode.FullScreen)
        return;
      // We must store the window state to be able to correctly restore the clients bounds if the user was in maximized mode
      // when switching to fullscreen
      _previousWindowState = WindowState;
      if (WindowState != FormWindowState.Normal)
        return;
      // Only store size and position if we are in windowed mode and not maximized. The size for all other modes/states
      // is obvious, only those two values are interesting to be restored on a mode switch from fullscreen to windowed.
      _previousWindowLocation = Location;
      _previousWindowClientSize = ClientSize;
    }

    public void StopUI()
    {
      ServiceRegistration.Get<ILogger>().Debug("DirectX MainForm: Stoping UI");
      StopRenderThread();
      PlayersHelper.ReleaseGUIResources();
      ServiceRegistration.Get<ContentManager>().Free();
    }

    public void StartUI()
    {
      ServiceRegistration.Get<ILogger>().Debug("DirectX MainForm: Starting UI");
      GraphicsDevice.Reset();
      PlayersHelper.ReallocGUIResources();
      StartRenderThread_Async();
    }

    protected void AdaptToSize()
    {
      StoreClientBounds();
      StopUI();
      SkinContext.WindowSize = ClientSize;
      StartUI();
    }

    protected void ShowMouseCursor(bool show)
    {
      if (show != _mouseHidden)
        return;
      _mouseHidden = !show;
      if (_mouseHidden)
        Cursor.Hide();
      else
        Cursor.Show();
    }

    /// <summary>
    /// Sets the TopMost property setting according to the current fullscreen setting
    /// and activation mode.
    /// </summary>
    protected void CheckTopMost()
    {
#if DEBUG
      TopMost = false;
#else
      TopMost = IsFullScreen && this == ActiveForm;
#endif
    }

    protected void StartRenderThread_Async()
    {
      if (_renderThread != null)
        throw new Exception("DirectX MainForm: Render thread already running");
      ServiceRegistration.Get<ILogger>().Debug("DirectX MainForm: Starting render thread");
      _renderThreadStopped = false;
      _renderThread = new Thread(RenderLoop) {Name = "DirectX Render Thread"};
      _renderThread.Start();
    }

    internal void StopRenderThread()
    {
      _renderThreadStopped = true;
      if (_renderThread == null)
        return;
      ServiceRegistration.Get<ILogger>().Debug("DirectX MainForm: Stoping render thread");
      _renderThread.Join();
      _renderThread = null;
    }

    private void RenderLoop()
    {
      // The render loop is restarted after toggle windowed / fullscreen
      // Make sure we invalidate all windows so the layout is re-done 
      // Big window layout does not fit into small window
      _screenManager.Reset();

      SkinContext.RenderThread = Thread.CurrentThread;

      try
      {
        GraphicsDevice.SetRenderState();
        while (!_renderThreadStopped)
        {
          bool shouldWait = GraphicsDevice.Render(true);
          if (shouldWait || !_hasFocus)
            Thread.Sleep(20);
        
          if (GraphicsDevice.DeviceLost)
            break;
        }
      }
      finally
      {
        SkinContext.RenderThread = null;
      }
      ServiceRegistration.Get<ILogger>().Debug("DirectX MainForm: Render thread stopped");
    }


    public void Start()
    {
      CheckTopMost();
      StartUI();
      ServiceRegistration.Get<ILogger>().Debug("DirectX MainForm: Running");
    }

    public void Shutdown()
    {
      Close();
    }

    public void Minimize()
    {
      WindowState = FormWindowState.Minimized;
      Screen screen = _screenManager.FocusedScreen;
      if (screen != null)
        screen.RemoveCurrentFocus();
    }

    public void Restore()
    {
      WindowState = _mode == ScreenMode.NormalWindowed ? FormWindowState.Normal : FormWindowState.Maximized;
    }

    public void Hibernate()
    {
      ServiceRegistration.Get<ISystemStateService>().Hibernate();
    }

    public void SwitchMode(ScreenMode mode)
    {
      if (InvokeRequired)
      {
        Invoke(new SwitchModeDelegate(SwitchMode), mode);
        return;
      }

      ServiceRegistration.Get<ILogger>().Debug("DirectX MainForm: Switching mode to {0}", mode);
      bool newFullscreen = mode == ScreenMode.FullScreen;
      AppSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();

      // Already done, no need to do it twice
      if (mode == _mode)
        return;

      settings.FullScreen = newFullscreen;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);

      StopUI();

      _adaptToSizeEnabled = false;
      try
      {
        // Must be done before reset. Otherwise we will lose the device after reset.
        if (newFullscreen)
        {
          StoreClientBounds();
          SwitchToFullscreen();
        }
        else
          SwitchToWindowedSize(_previousWindowLocation, _previousWindowClientSize, _previousWindowState == FormWindowState.Maximized);
      }
      finally
      {
        _adaptToSizeEnabled = true;
      }
      SkinContext.WindowSize = ClientSize;

      CheckTopMost();
      Update();
      Activate();

      StartUI();
    }

    public bool IsFullScreen
    {
      get { return _mode == ScreenMode.FullScreen; }
    }

    public bool IsScreenSaverActive
    {
      get { return _isScreenSaverActive; }
    }

    public bool IsScreenSaverEnabled
    {
      get { return _isScreenSaverEnabled; }
      set { _isScreenSaverEnabled = value; }
    }

    public IList<string> DisplayModes
    {
      get
      {
        IList<string> result = new List<string>();
        foreach (DisplayMode mode in GraphicsDevice.GetDisplayModes())
          result.Add(ToString(mode));
        return result;
      }
    }

    public IntPtr MainWindowHandle
    {
      get { return Handle; }
    }

    protected static string ToString(DisplayMode mode)
    {
      return string.Format("{0}x{1}@{2}", mode.Width, mode.Height, mode.RefreshRate);
    }

    private void OnApplicationIdle(object sender, EventArgs e)
    {
      try
      {
        // Screen saver
        IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
        if (_isScreenSaverEnabled)
          _isScreenSaverActive = DateTime.Now - inputManager.LastMouseUsageTime > SCREENSAVER_TIMEOUT &&
              DateTime.Now - inputManager.LastInputTime > SCREENSAVER_TIMEOUT;
        else
          _isScreenSaverActive = false;

        if (IsFullScreen)
          // If we are in fullscreen mode, we may control the mouse cursor
          ShowMouseCursor(inputManager.IsMouseUsed);
        else
          // Reset it to visible state, if state was switched
          ShowMouseCursor(true);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error occured in Idle handler", ex);
      }
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      try
      {
        logger.Debug("DirectX MainForm: Stopping");
        StopUI();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error occured in FormClosing handler", ex);
      }
      logger.Debug("DirectX MainForm: Closing");
      // We have to call ExitThread() explicitly because the application was started without
      // setting the MainWindow, which would have added an event handler which calls
      // Application.ExitThread() for us
      Application.ExitThread();
    }

    private void MainForm_MouseWheel(object sender, MouseEventArgs e)
    {
      if (_renderThreadStopped)
        return;
      int numDetents = e.Delta / 120;
      if (numDetents == 0)
        return;

      ServiceRegistration.Get<IInputManager>().MouseWheel(numDetents);
    }

    private void MainForm_MouseMove(object sender, MouseEventArgs e)
    {
      try
      {
        if (_renderThreadStopped)
          return;
        if (e.X == _previousMousePosition.X && e.Y == _previousMousePosition.Y)
          return;
        if (_previousMousePosition.X < 0 && _previousMousePosition.Y < 0)
        {
          _previousMousePosition.X = e.X;
          _previousMousePosition.Y = e.Y;
          return;
        }
        _previousMousePosition.X = e.X;
        _previousMousePosition.Y = e.Y;
        float x = e.X;
        float y = e.Y;
        ServiceRegistration.Get<IInputManager>().MouseMove(x, y);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error occured in MouseMove handler", ex);
      }
    }

    private void MainForm_KeyDown(object sender, KeyEventArgs e)
    {
      try
      {
        // We'll handle special keys here
        Key key = InputMapper.MapSpecialKey(e.KeyCode, e.Alt);
        if (key != Key.None)
        {
          IInputManager manager = ServiceRegistration.Get<IInputManager>();
          manager.KeyPress(key);
          e.Handled = true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error occured in KeyDown handler", ex);
      }
    }

    private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
    {
      try
      {
        // We'll handle printable keys here
        Key key = InputMapper.MapPrintableKeys(e.KeyChar);
        if (key != Key.None)
        {
          IInputManager manager = ServiceRegistration.Get<IInputManager>();
          manager.KeyPress(key);
          e.Handled = true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error occured in KeyPress handler", ex);
      }
    }

    private void MainForm_MouseClick(object sender, MouseEventArgs e)
    {
      try
      {
        IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
        inputManager.MouseClick(e.Button);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error occured in MouseClick handler", ex);
      }
    }

    private void timer_Tick(object sender, EventArgs e)
    {
      // Avoid multiple threads in here.
      if (Monitor.TryEnter(_reclaimDeviceSyncObj))
        try
        {
          if (GraphicsDevice.DeviceLost)
          {
            StopRenderThread();
            if (_hasFocus)
            {
              if (GraphicsDevice.ReclaimDevice())
              {
                GraphicsDevice.DeviceLost = false;
                StartRenderThread_Async();
              }
            }
          }
        }
        finally
        {
          Monitor.Exit(_reclaimDeviceSyncObj);
        }
    }

    private void MainForm_Activated(object sender, EventArgs e)
    {
      CheckTopMost();
    }

    private void MainForm_Deactivate(object sender, EventArgs e)
    {
      CheckTopMost();
    }

    protected override void WndProc(ref Message m)
    {
      //const long WM_SIZING = 0x214;
      //const int WMSZ_LEFT = 1;
      //const int WMSZ_RIGHT = 2;
      //const int WMSZ_TOP = 3;
      //const int WMSZ_TOPLEFT = 4;
      //const int WMSZ_TOPRIGHT = 5;
      //const int WMSZ_BOTTOM = 6;
      //const int WMSZ_BOTTOMLEFT = 7;
      //const int WMSZ_BOTTOMRIGHT = 8;
      const int WM_SYSCHAR = 0x106;

      // Hande 'beep'
      if (m.Msg == WM_SYSCHAR)
        return;

      // Albert, 2010-03-13: The following code can be used to make the window always maintain a fixed aspect ratio.
      // It was commented out because it doesn't really help. At least in the fullscreen mode, the aspect ratio is determined
      // by the screen and not by any other desired aspect ratio. I think we should not use the code, that's why I comment
      // it out.
      // When it should be used, the field _fixed_aspect_ratio must be initialized with a sensible value, for example
      // the aspect ratio from the skin.
      //if (m.Msg == WM_SIZING && m.HWnd == Handle)
      //{
      //  if (WindowState == FormWindowState.Normal)
      //  {
      //    Rect r = (Rect) Marshal.PtrToStructure(m.LParam, typeof(Rect));

      //    // Calc the border offset
      //    Size offset = new Size(Width - ClientSize.Width, Height - ClientSize.Height);

      //    // Calc the new dimensions.
      //    float wid = r.Right - r.Left - offset.Width;
      //    float hgt = r.Bottom - r.Top - offset.Height;
      //    // Calc the new aspect ratio.
      //    float new_aspect_ratio = hgt / wid;

      //    // See if the aspect ratio is changing.
      //    if (_fixed_aspect_ratio != new_aspect_ratio)
      //    {
      //      Int32 dragBorder = m.WParam.ToInt32();
      //      // To decide which dimension we should preserve,
      //      // see what border the user is dragging.
      //      if (dragBorder == WMSZ_TOPLEFT || dragBorder == WMSZ_TOPRIGHT ||
      //          dragBorder == WMSZ_BOTTOMLEFT || dragBorder == WMSZ_BOTTOMRIGHT)
      //      {
      //        // The user is dragging a corner.
      //        // Preserve the bigger dimension.
      //        if (new_aspect_ratio > _fixed_aspect_ratio)
      //          // It's too tall and thin. Make it wider.
      //          wid = hgt / _fixed_aspect_ratio;
      //        else
      //          // It's too short and wide. Make it taller.
      //          hgt = wid * _fixed_aspect_ratio;
      //      }
      //      else if (dragBorder == WMSZ_LEFT || dragBorder == WMSZ_RIGHT)
      //        // The user is dragging a side.
      //        // Preserve the width.
      //        hgt = wid * _fixed_aspect_ratio;
      //      else if (dragBorder == WMSZ_TOP || dragBorder == WMSZ_BOTTOM)
      //        // The user is dragging the top or bottom.
      //        // Preserve the height.
      //        wid = hgt / _fixed_aspect_ratio;
      //      // Figure out whether to reset the top/bottom
      //      // and left/right.
      //      // See if the user is dragging the top edge.
      //      if (dragBorder == WMSZ_TOP || dragBorder == WMSZ_TOPLEFT ||
      //          dragBorder == WMSZ_TOPRIGHT)
      //        // Reset the top.
      //        r.Top = r.Bottom - (int)(hgt + offset.Height);
      //      else
      //        // Reset the bottom.
      //        r.Bottom = r.Top + (int)(hgt + offset.Height);
      //      // See if the user is dragging the left edge.
      //      if (dragBorder == WMSZ_LEFT || dragBorder == WMSZ_TOPLEFT ||
      //          dragBorder == WMSZ_BOTTOMLEFT)
      //        // Reset the left.
      //        r.Left = r.Right - (int)(wid + offset.Width);
      //      else
      //        // Reset the right.
      //        r.Right = r.Left + (int)(wid + offset.Width);
      //      // Update the Message object's LParam field.
      //      Marshal.StructureToPtr(r, m.LParam, true);
      //    }
      //  }
      //}
      // Send windows message through the system if any component needs to access windows messages
      WindowsMessaging.BroadcastWindowsMessage(ref m);
      base.WndProc(ref m);
    }

    protected override void OnResizeEnd(EventArgs e)
    {
      // Also called on window movement
      base.OnResizeEnd(e);
      if (_adaptToSizeEnabled && ClientSize != _previousWindowClientSize)
        AdaptToSize();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
      base.OnSizeChanged(e);
      // This method override is only necessary to capture the window state change event. All other cases aren't interesting here.
      if (_adaptToSizeEnabled && WindowState != FormWindowState.Minimized && _previousWindowState != WindowState)
        AdaptToSize();
    }

    protected override void OnGotFocus(EventArgs e)
    {
      base.OnGotFocus(e);
      _hasFocus = true;
    }

    protected override void OnLostFocus(EventArgs e)
    {
      base.OnLostFocus(e);
      _hasFocus = false;
    }

  }
}
