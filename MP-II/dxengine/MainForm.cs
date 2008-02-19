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
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.ExifReader;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MenuManager;
using MediaPortal.Core.Players;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Collections;

using MediaPortal.Core.UserManagement;
using MediaPortal.Core.MediaManager;
using MediaPortal.Core.WindowManager;
using MediaPortal.Core.Importers;
using MediaPortal.Core.AutoPlay;

using MediaPortal.Services.ExifReader;
using MediaPortal.Services.InputManager;
using MediaPortal.Services.MenuManager;
using MediaPortal.Services.UserManagement;
using MediaPortal.Services.AutoPlay;

using SkinEngine;
using SkinEngine.Commands;
using SkinEngine.Fonts;
using SkinEngine.Players;
using SkinEngine.Skin;

namespace dxEngine
{
  // MainForm must be first in file otherwise can't open in designer
  public partial class MainForm : Form, IApplication
  {
    private Thread _renderThread;
    private GraphicsDevice _directX;
    private bool _isRunning;
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

    public MainForm()
    {
      //**********************************************************
      //following stuff should be dynamicly build offcourse
      //ILogger logger = new FileLogger(@"log\mediaportal.log", LogLevel.All);
      //ServiceScope.Add(logger); //<T> parameter is unnecessary when T = type of variable
      //logger.Debug("Application: starting");


      ServiceScope.Add<IApplication>(this);
      ServiceScope.Add<IInputMapper>(new InputMapper());

      ServiceScope.Get<ILogger>().Debug("Application: create ICommandBuilder service");
      CommandBuilder cmdBuilder = new CommandBuilder();
      ServiceScope.Add<ICommandBuilder>(cmdBuilder);

      ServiceScope.Get<ILogger>().Debug("Application: create IInputManager service");
      InputManager inputManager = new InputManager();
      ServiceScope.Add<IInputManager>(inputManager);

      ServiceScope.Get<ILogger>().Debug("Application: create IMenuManager service");

      MenuCollection menuCollection = new MenuCollection();
      ServiceScope.Add<IMenuCollection>(menuCollection);

      MenuBuilder menuBuilder = new MenuBuilder();
      ServiceScope.Add<IMenuBuilder>(menuBuilder);

      ServiceScope.Get<ILogger>().Debug("Application: create IWindowManager service");
      WindowManager windowManager = new WindowManager();
      ServiceScope.Add<IWindowManager>(windowManager);


      ServiceScope.Get<ILogger>().Debug("Application: create PlayerCollection service");
      MediaPlayers players = new MediaPlayers();
      ServiceScope.Add<PlayerCollection>(players);

      ServiceScope.Get<ILogger>().Debug("Application: create UserService service");
      UserService userservice = new UserService();
      ServiceScope.Add<IUserService>(userservice);

      ServiceScope.Get<ILogger>().Debug("Application: create AutoPlay service");
      AutoPlay autoplayservice = new AutoPlay();    
      ServiceScope.Add<IAutoPlay>(autoplayservice);

      
      //**********************************************************

      _previousMousePosition = new Point(-1, -1);
      InitializeComponent();
      CheckForIllegalCrossThreadCalls = false;

      ServiceScope.Get<ILogger>().Debug("Application: load skin settings");
      //Loader loader = new Loader();
      //loader.LoadSkinSettings();

      Rectangle screen = Screen.PrimaryScreen.Bounds;
      float ar = screen.Width / ((float)screen.Height);
      if (false && ar >= 1.6)
      {
        float height = screen.Height;
        height *= 0.7f;

        if (height < SkinContext.Height)
        {
          height = SkinContext.Height;
        }
        float width = height * (16.0f / 9.0f);
        ClientSize = new Size((int)width, (int)height);
        fixed_aspect_ratio = 9.0f / 16.0f;
      }
      else
      {
        ClientSize = new Size((int)SkinContext.Width, (int)SkinContext.Height);
        fixed_aspect_ratio = 3.0f / 4.0f;
      }
      // this.ClientSize = new Size(SkinContext.Width, SkinContext.Height);
      // fixed_aspect_ratio = 3.0f / 4.0f;
      AppSettings settings = new AppSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);

      // Remember prev size
      _previousClientSize = ClientSize;

      // Set-up for fullscreen
      if (settings.FullScreen)
      {
        Location = new Point(0, 0);
        ClientSize = Screen.PrimaryScreen.Bounds.Size;
        FormBorderStyle = FormBorderStyle.None;
        Text = "";
        MinimizeBox = false;
        MaximizeBox = false;
        ControlBox = false;
        BackColor = Color.Black;
        TopMost = true;
        ShowInTaskbar = false;
        _mode = ScreenMode.ExclusiveMode;
        _displaySetting = GraphicsDevice.DesktopDisplayMode;

        Win32API.EnableStartBar(false);
        Win32API.ShowStartBar(false);
      }
      _windowState = WindowState;
    }


    private void Form1_Load(object sender, EventArgs e)
    {
      Text = "Mediaportal II";
      SkinContext.Form = this;
      _previousPosition = Location;

      ServiceScope.Get<ILogger>().Debug("Application: initialize directx");
      AppSettings settings = new AppSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      _directX = new GraphicsDevice(this, settings.FullScreen);
      ServiceScope.Get<ILogger>().Debug("Application: load skin");
      WindowManager manager = (WindowManager)ServiceScope.Get<IWindowManager>();
      manager.LoadSkin();
      ServiceScope.Get<ILogger>().Debug("Application: start render thread");
      _renderThread = new Thread(RenderLoop);
      _renderThread.Name = "DirectX Render Thread";
      _renderThread.Start();
      ServiceScope.Get<ILogger>().Debug("Application: running");

      // The form is active, so let's start listening on AutoPlay events
      ServiceScope.Get<IAutoPlay>().StartListening(this.Handle);
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
      ServiceScope.Get<ILogger>().Debug("Application: closing");
      ServiceScope.Get<ILogger>().Debug("Application: stop renderthread");
      _isRunning = false;
      if (_renderThread != null)
      {
        _renderThread.Join();
      }
      ServiceScope.Get<ILogger>().Debug("Application: stop players");
      ServiceScope.Get<PlayerCollection>().Dispose();
      ServiceScope.Get<ILogger>().Debug("Application: dispose directx");
      _directX.Dispose();
      _directX = null;
      _renderThread = null;
      Win32API.EnableStartBar(true);
      Win32API.ShowStartBar(true);
      ServiceScope.Get<ILogger>().Debug("Application: stopping.");
    }

    private void RenderLoop()
    {
      _fpsTimer = DateTime.Now;
      _fpsCounter = 0.0f;
      _isRunning = true;
      SkinContext.IsRendering = true;
      while (_isRunning)
      {
        Render();
        TimeSpan ts = DateTime.Now - _fpsTimer;
        if (ts.TotalSeconds >= 1.0f)
        {
          float secs = (float)ts.TotalSeconds;
          _fpsCounter /= secs;
          //this.Text = "fps:" + _fpsCounter.ToString("f2") + " "+ _hasFocus.ToString();
          _fpsCounter = 0;
          _fpsTimer = DateTime.Now;
          if (GraphicsDevice.DeviceLost)
          {
            break;
          }
        }
      }
      SkinContext.IsRendering = false;
      ServiceScope.Get<ILogger>().Debug("Application: renderthread stopped.");
    }

    /// <summary>
    /// Renders the screen
    /// </summary>
    public void Render()
    {
      bool shouldWait = GraphicsDevice.Render(true);
      if (shouldWait || !_hasFocus)
      {
        Thread.Sleep(100);
      }
      _fpsCounter += 1.0f;
    }

    private void MainForm_MouseMove(object sender, MouseEventArgs e)
    {
      if (!_isRunning)
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
      //x *= (((float)SkinContext.Width) / ((float)this.ClientSize.Width));
      //y *= (((float)SkinContext.Height) / ((float)this.ClientSize.Height));
      //x *= (SkinContext.Width / GraphicsDevice.Width);
      //y *= (SkinContext.Height / GraphicsDevice.Height);
      //      this.Text = String.Format("{0},{1}", x.ToString("f2"), y.ToString("f2"));
      ServiceScope.Get<IInputManager>().MouseMove(x, y);
    }


    private void MainForm_KeyUp(object sender, KeyEventArgs e) { }

    private void MainForm_KeyDown(object sender, KeyEventArgs e)
    {
      ServiceScope.Get<ILogger>().Info("keydown:{0}", e.KeyCode);
      IInputMapper mapper = ServiceScope.Get<IInputMapper>();
      Key key = mapper.Map(e.KeyCode, e.Alt);
      if (key != Key.None)
      {
        IInputManager manager = ServiceScope.Get<IInputManager>();
        manager.KeyPressed(key);
        e.Handled = true;
      }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
      IInputMapper mapper = ServiceScope.Get<IInputMapper>();
      Key key = mapper.Map(e.KeyChar);
      if (key != Key.None)
      {
        IInputManager manager = ServiceScope.Get<IInputManager>();
        manager.KeyPressed(key);
        e.Handled = true;
      }
      if (e.KeyChar == 'i')
      {
        PlayerCollection coll = ServiceScope.Get<PlayerCollection>();
        if (coll.Count > 0)
        {
          VideoPlayer player = coll[0] as VideoPlayer;
          if (player != null)
          {
            if (player.Effect == null)
            {
              SkinEngine.Effects.EffectAsset effect = ContentManager.GetEffect("smartzoom");
              if (effect != null)
              {
                player.Effect = effect;
              }
            }
            else
            {
              player.Effect = null;
            }
          }
        }
      }
    }

    private void MainForm_MouseClick(object sender, MouseEventArgs e)
    {
      SkinContext.MouseUsed = true;
      if (e.Button == MouseButtons.Left)
      {
        ServiceScope.Get<IInputManager>().KeyPressed(Key.Enter);
      }
      if (e.Button == MouseButtons.Right)
      {
        ServiceScope.Get<IInputManager>().KeyPressed(Key.ContextMenu);
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

      if (m.Msg == WM_SIZING && m.HWnd == Handle)
      {
        if (WindowState == FormWindowState.Normal)
        {
          Rect r = (Rect)Marshal.PtrToStructure(m.LParam, typeof(Rect));

          // Get the current dimensions.
          float wid = r.Right - r.Left;
          float hgt = r.Bottom - r.Top;
          // Get the new aspect ratio.
          float new_aspect_ratio = hgt / wid;

          // The first time, save the form? aspect ratio.
          if (fixed_aspect_ratio == 0)
          {
            fixed_aspect_ratio = new_aspect_ratio;
          }
          // See if the aspect ratio is changing.
          if (fixed_aspect_ratio != new_aspect_ratio)
          {
            // To decide which dimension we should preserve,
            // see what border the user is dragging.
            if (m.WParam.ToInt32() == WMSZ_TOPLEFT || m.WParam.ToInt32() == WMSZ_TOPRIGHT ||
                m.WParam.ToInt32() == WMSZ_BOTTOMLEFT || m.WParam.ToInt32() == WMSZ_BOTTOMRIGHT)
            {
              // The user is dragging a corner.
              // Preserve the bigger dimension.
              if (new_aspect_ratio > fixed_aspect_ratio)
              {
                // It? too tall and thin. Make it wider.
                wid = hgt / fixed_aspect_ratio;
              }
              else
              {
                // It? too short and wide. Make it taller.
                hgt = wid * fixed_aspect_ratio;
              }
            }
            else if (m.WParam.ToInt32() == WMSZ_LEFT || m.WParam.ToInt32() == WMSZ_RIGHT)
            {
              // The user is dragging a side.
              // Preserve the width.
              hgt = wid * fixed_aspect_ratio;
            }
            else if (m.WParam.ToInt32() == WMSZ_TOP || m.WParam.ToInt32() == WMSZ_BOTTOM)
            {
              // The user is dragging the top or bottom.
              // Preserve the height.
              wid = hgt / fixed_aspect_ratio;
            }
            // Figure out whether to reset the top/bottom
            // and left/right.
            // See if the user is dragging the top edge.
            if (m.WParam.ToInt32() == WMSZ_TOP || m.WParam.ToInt32() == WMSZ_TOPLEFT ||
                m.WParam.ToInt32() == WMSZ_TOPRIGHT)
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
            if (m.WParam.ToInt32() == WMSZ_LEFT || m.WParam.ToInt32() == WMSZ_TOPLEFT ||
                m.WParam.ToInt32() == WMSZ_BOTTOMLEFT)
            {
              // Reset the left.
              r.Left = r.Right - (int)(wid);
            }
            else
            {
              // Reset the right.
              r.Right = r.Left + (int)(wid);
            }
            // Update the Message object? LParam field.
            Marshal.StructureToPtr(r, m.LParam, true);
          }
        }
      }
      ServiceScope.Get<PlayerCollection>().OnMessage(m);
      base.WndProc(ref m);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
      base.OnSizeChanged(e);
      ServiceScope.Get<ILogger>().Debug("Application: OnSizeChanged  {0} {1}", Bounds.ToString(), WindowState);

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
      ServiceScope.Get<ILogger>().Debug("Application: OnResizeEnd {0} {1}", Bounds.ToString(), WindowState);

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
        // ServiceScope.Get<ILogger>().Debug("Application: stop render thread");
        _isRunning = false;
        if (_renderThread != null)
        {
          _renderThread.Join();
          _renderThread = null;
        }

        ServiceScope.Get<PlayerCollection>().ReleaseResources();
        //ServiceScope.Get<ILogger>().Debug("Application: dispose resources");
        //ServiceScope.Get<PlayerCollection>().Dispose();

        FontManager.Free();
        ContentManager.Free();
        //
        ServiceScope.Get<ILogger>().Debug("Application: reset directx");

        if (WindowState != FormWindowState.Minimized)
        {
          GraphicsDevice.Reset(this, (_mode == ScreenMode.ExclusiveMode), string.Empty);
          ServiceScope.Get<ILogger>().Debug("Application: allocate fonts");
          FontManager.Alloc();
          ServiceScope.Get<IWindowManager>().CurrentWindow.Reset();

          ServiceScope.Get<ILogger>().Debug("Application: start render thread");
          _renderThread = new Thread(RenderLoop);
          _renderThread.Start();
        }
        ServiceScope.Get<PlayerCollection>().ReallocResources();
      }
    }

    public void SwitchMode(ScreenMode mode, FPS fps)
    {
      ServiceScope.Get<ILogger>().Debug("Application: SwitchMode({0},{1})", mode, fps);
      bool maximize = (mode == ScreenMode.ExclusiveMode) || (mode == ScreenMode.FullScreenWindowed);
      string displaySetting;
      AppSettings settings = new AppSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);

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

      if (maximize)
      {
        ServiceScope.Get<ILogger>().Debug("Application: ClientSize {0}", ClientSize);
        _previousClientSize = ClientSize;
      }


      ServiceScope.Get<ILogger>().Debug("Application: stop renderthread");

      settings.FullScreen = maximize;
      ServiceScope.Get<ISettingsManager>().Save(settings);

      ServiceScope.Get<ILogger>().Debug("Application: stop renderthread");
      _isRunning = false;
      _renderThread.Join();
      ServiceScope.Get<ILogger>().Debug("Application: dispose resources");
      ServiceScope.Get<PlayerCollection>().ReleaseResources();

      FontManager.Free();
      ContentManager.Free();

      // Must be done before reset. Otherwise we will loose device after reset.
      if (maximize)
      {
        Location = new Point(0, 0);
        ClientSize = Screen.PrimaryScreen.Bounds.Size;
        FormBorderStyle = FormBorderStyle.None;
        MinimizeBox = false;
        MaximizeBox = false;
        ControlBox = false;
        BackColor = Color.Black;
        TopMost = true;
        ShowInTaskbar = false;

        // Hide start menu
        Win32API.EnableStartBar(false);
        Win32API.ShowStartBar(false);
      }
      else
      {
        WindowState = FormWindowState.Normal;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        ControlBox = true;
        ClientSize = _previousClientSize;
        Location = _previousPosition;

        // Show start menu
        Win32API.EnableStartBar(true);
        Win32API.ShowStartBar(true);

        Update();
        Activate();
      }

      ServiceScope.Get<ILogger>().Debug("Application: switch mode maximize = {0},  mode = {1}, displaySetting = {2}", maximize, mode, displaySetting);
      ServiceScope.Get<ILogger>().Debug("Application: reset directx");

      GraphicsDevice.Reset(this, (mode == ScreenMode.ExclusiveMode), displaySetting);

      ServiceScope.Get<ILogger>().Debug("Application: allocate fonts");
      FontManager.Alloc();

      ServiceScope.Get<PlayerCollection>().ReallocResources();

      ServiceScope.Get<ILogger>().Debug("Application: start renderthread");
      _renderThread = new Thread(RenderLoop);
      _renderThread.Start();
    }

    public bool IsFullScreen
    {
      get { return (_mode == ScreenMode.FullScreenWindowed) || (_mode == ScreenMode.ExclusiveMode); }
    }

    public bool RefreshRateControlEnabled
    {
      get
      {
        AppSettings settings = new AppSettings();
        ServiceScope.Get<ISettingsManager>().Load(settings);

        return settings.RefreshRateControl;
      }
      set
      {
        AppSettings settings = new AppSettings();
        ServiceScope.Get<ISettingsManager>().Load(settings);
        settings.RefreshRateControl = value;
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }
    }

    public ItemsCollection DisplayModes
    {
      get { return GraphicsDevice.DisplayModes; }
    }

    public void setDisplayMode(FPS fps, string displaymode)
    {
      AppSettings settings = new AppSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);

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

    public string getDisplayMode(FPS fps)
    {
      AppSettings settings = new AppSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);

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

    private static bool reentrant = false;

    private void timer1_Tick(object sender, EventArgs e)
    {
      if (reentrant)
      {
        return;
      }

      try
      {
        reentrant = true;
        if (GraphicsDevice.DeviceLost)
        {
          if (_renderThread != null)
          {
            _renderThread.Join();
            _renderThread = null;
          }
          if (_hasFocus)
          {
            if (GraphicsDevice.ReclaimDevice())
            {
              FontManager.Alloc();
              _renderThread = new Thread(RenderLoop);
              GraphicsDevice.DeviceLost = false;
              _renderThread.Start();
            }
          }
        }
      }
      finally
      {
        reentrant = false;
      }
    }
  }

  internal struct Rect
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
  } ;

  public class AppSettings
  {
    private bool _fullScreen;
    private bool _refreshRateControl;
    private string _FPS24;
    private string _FPS25;
    private string _FPS30;
    private string _FPSDefault;

    [Setting(SettingScope.User, false)]
    public bool FullScreen
    {
      get { return _fullScreen; }
      set { _fullScreen = value; }
    }
    [Setting(SettingScope.User, false)]
    public bool RefreshRateControl
    {
      get { return _refreshRateControl; }
      set { _refreshRateControl = value; }
    }
    [Setting(SettingScope.Global, "")]
    public string FPS24
    {
      get { return _FPS24; }
      set { _FPS24 = value; }
    }
    [Setting(SettingScope.Global, "")]
    public string FPS25
    {
      get { return _FPS25; }
      set { _FPS25 = value; }
    }
    [Setting(SettingScope.Global, "")]
    public string FPS30
    {
      get { return _FPS30; }
      set { _FPS30 = value; }
    }
    [Setting(SettingScope.Global, "")]
    public string FPSDefault
    {
      get { return _FPSDefault; }
      set { _FPSDefault = value; }
    }
  }

  public static class Win32API
  {


    public static void Show(string className, string windowName, bool visible)
    {
      uint i = FindWindow(ref className, ref windowName);
      if (visible)
      {
        ShowWindow(i, 5);
      }
      else
      {
        ShowWindow(i, 0);
      }
    }

    public static void Enable(string className, string windowName, bool enable)
    {
      uint i = FindWindow(ref className, ref windowName);
      if (enable)
      {
        EnableWindow(i, -1);
      }
      else
      {
        EnableWindow(i, 0);
      }
    }

    public static void ShowStartBar(bool visible)
    {
      try
      {
        Show("Shell_TrayWnd", "", visible);
      }
      catch (Exception) { }
    }

    public static void EnableStartBar(bool enable)
    {
      try
      {
        Enable("Shell_TrayWnd", "", enable);
      }
      catch (Exception) { }
    }

    [DllImportAttribute("user32", EntryPoint = "FindWindowA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern uint FindWindow([MarshalAs(UnmanagedType.VBByRefStr)] ref string lpclassName, [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpwindowName);

    [DllImport("user32", SetLastError = true)]
    private static extern uint ShowWindow(uint _hwnd, int _showCommand);

    [DllImportAttribute("user32", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern int EnableWindow(uint hwnd, int fEnable);
  }
}
