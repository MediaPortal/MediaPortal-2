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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Control.InputManager;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.Settings;
using MediaPortal.Presentation.Screen;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.ScreenManagement;
using MediaPortal.SkinEngine.SkinManagement;

using MediaPortal.SkinEngine.Settings;
using SlimDX.Direct3D9;

namespace MediaPortal.SkinEngine.GUI
{
  // MainForm must be first in file otherwise can't open in designer
  public partial class MainForm : Form, IScreenControl
  {
    private Thread _renderThread;
    private GraphicsDevice _directX;
    private bool _renderThreadStopped;
    private float _fpsCounter;
    private DateTime _fpsTimer;
    private float fixed_aspect_ratio = 0;
    private FormWindowState _windowState;
    private Size _previousClientSize;
    private Point _previousPosition;
    private Point _previousMousePosition;
    private ScreenMode _mode = ScreenMode.NormalWindowed;
    private bool _hasFocus = false;
    private string _displaySetting;
    private ScreenManager _screenManager;

    public MainForm(ScreenManager screenManager)
    {
      _screenManager = screenManager;

      ServiceScope.Get<ILogger>().Debug("Registering DirectX MainForm as IScreenControl service");
      ServiceScope.Add<IScreenControl>(this);

      InitializeComponent();
      CheckForIllegalCrossThreadCalls = false;

      SkinContext.Form = this;
      AppSettings appSettings = ServiceScope.Get<ISettingsManager>().Load<AppSettings>();

      _previousMousePosition = new Point(-1, -1);
      ClientSize = new Size(SkinContext.SkinWidth, SkinContext.SkinHeight);
      fixed_aspect_ratio = SkinContext.SkinHeight/(float) SkinContext.SkinWidth;

      // Remember prev size
      _previousClientSize = ClientSize;

      // Setup for fullscreen
      if (appSettings.FullScreen)
      {
        Location = new Point(0, 0);
        // FIXME Albert78: Don't use PrimaryScreen but the screen MP should be displayed on
        ClientSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
        FormBorderStyle = FormBorderStyle.None;
        _mode = ScreenMode.ExclusiveMode;
      }
      _windowState = WindowState;

      // GraphicsDevice has to be initialized after the form was sized correctly
      ServiceScope.Get<ILogger>().Debug("DirectX MainForm: Initialize DirectX");
      _directX = new GraphicsDevice(this, appSettings.FullScreen);

      _displaySetting = GraphicsDevice.DesktopDisplayMode;
    }

    public void Start()
    {
      CheckTopMost();

      // Start render thread before we show first screen, because the render thread does
      // an invalidate, we don't want a double invalidate.
      StartRenderThread_Async();

      ServiceScope.Get<ILogger>().Debug("DirectX MainForm: Running");
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      logger.Debug("DirectX MainForm: Stopping");
      StopRenderThread();
      logger.Debug("DirectX MainForm: Exit screen manager");
      _screenManager.Dispose();
      _screenManager.Exit();
      logger.Debug("DirectX MainForm: Dispose DirectX");
      _directX.Dispose();
      _directX = null;
      logger.Debug("DirectX MainForm: Closing");
      // We have to call ExitThread() explicitly because the application was started without
      // setting the MainForm, which would have added an event handler which calls
      // Application.ExitThread() for us
      Application.ExitThread();
    }

    private void RenderLoop()
    {
      // The render loop is restarted after toggle windowed / fullscreen
      // Make sure we invalidate all windows so the layout is re-done 
      // Big window layout does not fit into small window ;-)
      _screenManager.Reset();

      _fpsTimer = DateTime.Now;
      _fpsCounter = 0;
      SkinContext.IsRendering = true;

      try
      {
        GraphicsDevice.SetRenderState();
        while (!_renderThreadStopped)
        {
          bool shouldWait = GraphicsDevice.Render(true);
          if (shouldWait || !_hasFocus)
          {
            Thread.Sleep(100);
          }
          _fpsCounter += 1.0f;
          TimeSpan ts = DateTime.Now - _fpsTimer;
          if (ts.TotalSeconds >= 1.0f)
          {
            float secs = (float)ts.TotalSeconds;
            _fpsCounter /= secs;
            //Trace.WriteLine("fps:" + _fpsCounter.ToString("f2") + " "+ _hasFocus.ToString());
            _fpsCounter = 0;
            _fpsTimer = DateTime.Now;
            if (GraphicsDevice.DeviceLost)
            {
              break;
            }
          }
        }
      }
      finally
      {
        SkinContext.IsRendering = false;
      }
      ServiceScope.Get<ILogger>().Debug("DirectX MainForm: Render thread stopped");
    }


    private void MainForm_MouseMove(object sender, MouseEventArgs e)
    {
      if (_renderThreadStopped)
      {
        return;
      }
      if (e.X == _previousMousePosition.X && e.Y == _previousMousePosition.Y)
      {
        return;
      }
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
      ServiceScope.Get<IInputManager>().MouseMove(x, y);
    }

    private void MainForm_KeyDown(object sender, KeyEventArgs e)
    {
      // We'll handle special keys here
      IInputMapper mapper = ServiceScope.Get<IInputMapper>();
      Key key = mapper.MapSpecialKey(e.KeyCode, e.Alt);
      if (key != Key.None)
      {
        IInputManager manager = ServiceScope.Get<IInputManager>();
        manager.KeyPress(key);
        e.Handled = true;
      }
    }

    private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
    {
      // We'll handle alpha-numeric keys here
      IInputMapper mapper = ServiceScope.Get<IInputMapper>();
      Key key = mapper.MapAlphaNumericKey(e.KeyChar);
      if (key != Key.None)
      {
        IInputManager manager = ServiceScope.Get<IInputManager>();
        manager.KeyPress(key);
        e.Handled = true;
      }
    }

    private void MainForm_MouseClick(object sender, MouseEventArgs e)
    {
      SkinContext.MouseUsed = true;
      if (e.Button == MouseButtons.Left)
      {
        ServiceScope.Get<IInputManager>().KeyPress(Key.Enter);
      }
      if (e.Button == MouseButtons.Right)
      {
        ServiceScope.Get<IInputManager>().KeyPress(Key.ContextMenu);
      }
    }

    protected override void WndProc(ref Message m)
    {
      const long WM_SIZING = 0x214;
      const int WMSZ_LEFT = 1;
      const int WMSZ_RIGHT = 2;
      const int WMSZ_TOP = 3;
      const int WMSZ_TOPLEFT = 4;
      const int WMSZ_TOPRIGHT = 5;
      const int WMSZ_BOTTOM = 6;
      const int WMSZ_BOTTOMLEFT = 7;
      const int WMSZ_BOTTOMRIGHT = 8;
      const int WM_SYSCHAR = 0x106;

      // Hande 'beep'
      if (m.Msg == WM_SYSCHAR)
      {
        return;
      }

      if (m.Msg == WM_SIZING && m.HWnd == Handle)
      {
        if (WindowState == FormWindowState.Normal)
        {
          Rect r = (Rect) Marshal.PtrToStructure(m.LParam, typeof(Rect));

          // Get the current dimensions.
          float wid = r.Right - r.Left;
          float hgt = r.Bottom - r.Top;
          // Get the new aspect ratio.
          float new_aspect_ratio = hgt / wid;

          // See if the aspect ratio is changing.
          if (fixed_aspect_ratio != new_aspect_ratio)
          {
            Int32 dragBorder = m.WParam.ToInt32();
            // To decide which dimension we should preserve,
            // see what border the user is dragging.
            if (dragBorder == WMSZ_TOPLEFT || dragBorder == WMSZ_TOPRIGHT ||
                dragBorder == WMSZ_BOTTOMLEFT || dragBorder == WMSZ_BOTTOMRIGHT)
            {
              // The user is dragging a corner.
              // Preserve the bigger dimension.
              if (new_aspect_ratio > fixed_aspect_ratio)
              {
                // It's too tall and thin. Make it wider.
                wid = hgt / fixed_aspect_ratio;
              }
              else
              {
                // It's too short and wide. Make it taller.
                hgt = wid * fixed_aspect_ratio;
              }
            }
            else if (dragBorder == WMSZ_LEFT || dragBorder == WMSZ_RIGHT)
            {
              // The user is dragging a side.
              // Preserve the width.
              hgt = wid * fixed_aspect_ratio;
            }
            else if (dragBorder == WMSZ_TOP || dragBorder == WMSZ_BOTTOM)
            {
              // The user is dragging the top or bottom.
              // Preserve the height.
              wid = hgt / fixed_aspect_ratio;
            }
            // Figure out whether to reset the top/bottom
            // and left/right.
            // See if the user is dragging the top edge.
            if (dragBorder == WMSZ_TOP || dragBorder == WMSZ_TOPLEFT ||
                dragBorder == WMSZ_TOPRIGHT)
            {
              // Reset the top.
              r.Top = r.Bottom - (int)(hgt);
            }
            else
            {
              // Reset the bottom.
              r.Bottom = r.Top + (int)(hgt);
            }
            // See if the user is dragging the left edge.
            if (dragBorder == WMSZ_LEFT || dragBorder == WMSZ_TOPLEFT ||
                dragBorder == WMSZ_BOTTOMLEFT)
            {
              // Reset the left.
              r.Left = r.Right - (int)(wid);
            }
            else
            {
              // Reset the right.
              r.Right = r.Left + (int)(wid);
            }
            // Update the Message object's LParam field.
            Marshal.StructureToPtr(r, m.LParam, true);
          }
        }
      }
      //FIXME Albert78: The hard coded OnMessage call has to be replaced by a dynamic approach
      //ServiceScope.Get<IPlayerCollection>().OnMessage(m);
      base.WndProc(ref m);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
      base.OnSizeChanged(e);
      //ServiceScope.Get<ILogger>().Debug("DirectX MainForm: OnSizeChanged {0} {1}", Bounds.ToString(), ScreenState);

      if (GraphicsDevice.DeviceLost || (_mode == ScreenMode.ExclusiveMode))
      {
        return;
      }
      if (ClientSize != _previousClientSize)
      {
        if (WindowState != _windowState)
        {
          _windowState = WindowState;
          OnResizeEnd(null);
        }
      }
    }

    protected override void OnResizeEnd(EventArgs e)
    {
      //ServiceScope.Get<ILogger>().Debug("DirectX MainForm: OnResizeEnd {0} {1}", Bounds.ToString(), ScreenState);

      if (GraphicsDevice.DeviceLost || (_mode == ScreenMode.ExclusiveMode))
      {
        base.OnResizeEnd(e);
        _previousClientSize = ClientSize;
        return;
      }
      if (ClientSize != _previousClientSize)
      {
        base.OnResizeEnd(e);
        _previousClientSize = ClientSize;
        //Trace.WriteLine("DirectX MainForm: Stop render thread");
        StopRenderThread();

        ServiceScope.Get<IPlayerCollection>().ReleaseResources();

        ContentManager.Free();

        //Trace.WriteLine("DirectX MainForm: Reset DirectX");

        if (WindowState != FormWindowState.Minimized)
        {
          GraphicsDevice.Reset((_mode == ScreenMode.ExclusiveMode), _displaySetting);

          //Trace.WriteLine("DirectX MainForm: Restart render thread");
          StartRenderThread_Async();
        }
        ServiceScope.Get<IPlayerCollection>().ReallocResources();
      }
    }

    public void SwitchMode(ScreenMode mode, FPS fps)
    {
      ServiceScope.Get<ILogger>().Debug("DirectX MainForm: SwitchMode({0}, {1})", mode, fps);
      bool oldFullscreen = IsFullScreen;
      bool newFullscreen = (mode == ScreenMode.ExclusiveMode) || (mode == ScreenMode.FullScreenWindowed);
      string displaySetting;
      AppSettings settings = ServiceScope.Get<ISettingsManager>().Load<AppSettings>();

      // Get the display setting
      switch (fps)
      {
        case FPS.FPS_24:
          displaySetting = settings.FPS24;
          break;
        case FPS.FPS_25:
          displaySetting = settings.FPS25;
          break;
        case FPS.FPS_30:
          displaySetting = settings.FPS30;
          break;
        case FPS.Default:
          displaySetting = settings.FPSDefault;
          break;
        case FPS.Desktop:
          displaySetting = GraphicsDevice.DesktopDisplayMode;
          break;
        default:
          displaySetting = string.Empty;
          break;
      }

      // Fallback if nothing is found
      if (mode == ScreenMode.ExclusiveMode && displaySetting == string.Empty)
        displaySetting = GraphicsDevice.DesktopDisplayMode;

      // Already done, no need to do it twice
      if (mode == ScreenMode.ExclusiveMode && _mode == ScreenMode.ExclusiveMode && _displaySetting.CompareTo(displaySetting) == 0)
        return;

      _displaySetting = displaySetting;
      _mode = mode;

      if (newFullscreen && !oldFullscreen)
      {
        _previousClientSize = ClientSize;
      }


      settings.FullScreen = newFullscreen;
      ServiceScope.Get<ISettingsManager>().Save(settings);

      StopRenderThread();
      ServiceScope.Get<ILogger>().Debug("DirectX MainForm: Release resources");
      ServiceScope.Get<IPlayerCollection>().ReleaseResources();

      ContentManager.Free();

      // Must be done before reset. Otherwise we will lose the device after reset.
      if (newFullscreen)
      {
        _previousPosition = Location;
        Location = new Point(0, 0);
        // FIXME Albert78: Don't use PrimaryScreen but the screen MP should be displayed on
        ClientSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
        FormBorderStyle = FormBorderStyle.None;
      }
      else
      {
        WindowState = FormWindowState.Normal;
        FormBorderStyle = FormBorderStyle.Sizable;
        ClientSize = _previousClientSize;
        Location = _previousPosition;
      }
      CheckTopMost();
      Update();
      Activate();

      ServiceScope.Get<ILogger>().Debug("DirectX MainForm: Switch mode maximize = {0},  mode = {1}, displaySetting = {2}", newFullscreen, mode, displaySetting);
      ServiceScope.Get<ILogger>().Debug("DirectX MainForm: Reset DirectX");

      GraphicsDevice.Reset(mode == ScreenMode.ExclusiveMode, displaySetting);

      ServiceScope.Get<IPlayerCollection>().ReallocResources();

      StartRenderThread_Async();
    }

    public bool IsFullScreen
    {
      get { return (_mode == ScreenMode.FullScreenWindowed) || (_mode == ScreenMode.ExclusiveMode); }
    }

    public bool RefreshRateControlEnabled
    {
      get
      {
        AppSettings settings = ServiceScope.Get<ISettingsManager>().Load<AppSettings>();

        return settings.RefreshRateControl;
      }
      set
      {
        AppSettings settings = ServiceScope.Get<ISettingsManager>().Load<AppSettings>();
        settings.RefreshRateControl = value;
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }
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

    public void SetDisplayMode(FPS fps, string displaymode)
    {
      AppSettings settings = ServiceScope.Get<ISettingsManager>().Load<AppSettings>();

      switch (fps)
      {
        case FPS.FPS_24:
          settings.FPS24 = displaymode;
          break;
        case FPS.FPS_25:
          settings.FPS25 = displaymode;
          break;
        case FPS.FPS_30:
          settings.FPS30 = displaymode;
          break;
        case FPS.Default:
          settings.FPSDefault = displaymode;
          break;
      }
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    public string GetDisplayMode(FPS fps)
    {
      AppSettings settings = ServiceScope.Get<ISettingsManager>().Load<AppSettings>();

      switch (fps)
      {
        case FPS.FPS_24:
          return settings.FPS24;
        case FPS.FPS_25:
          return settings.FPS25;
        case FPS.FPS_30:
          return settings.FPS30;
        case FPS.Default:
          return settings.FPSDefault;
        default:
          throw new ArgumentException("Illegal frame rate");
      }
    }

    protected static string ToString(DisplayMode mode)
    {
      return string.Format("{0}x{1}@{2}", mode.Width, mode.Height, mode.RefreshRate);
    }

    protected void StartRenderThread_Async()
    {
      if (_renderThread != null)
        throw new Exception("DirectX MainForm: Render thread already running");
      ServiceScope.Get<ILogger>().Debug("DirectX MainForm: Start render thread");
      _renderThreadStopped = false;
      _renderThread = new Thread(RenderLoop);
      _renderThread.Name = "DirectX Render Thread";
      _renderThread.Start();
    }

    protected void StopRenderThread()
    {
      _renderThreadStopped = true;
      if (_renderThread == null)
        return;
      ServiceScope.Get<ILogger>().Debug("DirectX MainForm: Stop render thread");
      _renderThread.Join();
      _renderThread = null;
    }

    private void MainForm_MouseUp(object sender, MouseEventArgs e) { }

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

    private readonly object _reclaimDeviceSyncObj = new object();

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

    /// <summary>
    /// Sets the TopMost property setting according to the current fullscreen setting
    /// and activation mode.
    /// </summary>
    protected void CheckTopMost()
    {
      TopMost = IsFullScreen && this == ActiveForm;
    }

    private void MainForm_Activated(object sender, EventArgs e)
    {
      CheckTopMost();
    }

    private void MainForm_Deactivate(object sender, EventArgs e)
    {
      CheckTopMost();
    }
  }

  internal struct Rect
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
  };
}
