#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Common.Logging;
using MediaPortal.UI.General;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Services.Players.VideoPlayerSynchronizationStrategies;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Settings;
using MediaPortal.UI.SkinEngine.Utils;
using SlimDX.Direct3D9;
using Screen = MediaPortal.UI.SkinEngine.ScreenManagement.Screen;

namespace MediaPortal.UI.SkinEngine.GUI
{
  public delegate void SwitchModeDelegate(ScreenMode mode);

  public partial class MainForm : Form, IScreenControl
  {
    protected delegate void Dlgt();

    /// <summary>
    /// Maximum time between frames when our render thread is synchronized to the video player thread.
    /// </summary>
    public static int RENDER_MAX_WAIT_FOR_VIDEO_FRAME_MS = 100;

    /// <summary>
    /// The maximum time for the video player thread to wait for the render thread when those threads are synchronized.
    /// </summary>
    public static int VIDEO_PLAYER_MAX_WAIT_FOR_RENDER_MS = 10;

    private const string SCREEN_SAVER_SCREEN = "ScreenSaver";

    private bool _renderThreadStopped;
    private ISlimDXVideoPlayer _synchronizedVideoPlayer = null;
    private readonly AutoResetEvent _videoRenderFrameEvent = new AutoResetEvent(false);
    private readonly AutoResetEvent _renderFinishedEvent = new AutoResetEvent(false);
    private bool _videoPlayerSuspended = false;
    private IVideoPlayerSynchronizationStrategy _videoPlayerSynchronizationStrategy = null;
    private Size _previousWindowClientSize;
    private Point _previousWindowLocation;
    private FormWindowState _previousWindowState;
    private Point _previousMousePosition;
    private ScreenMode _mode = ScreenMode.NormalWindowed;
    private bool _hasFocus = false;
    private readonly ScreenManager _screenManager;
    protected bool _isScreenSaverEnabled = true;
    protected bool _isScreenSaverActive = false;
    protected ScreenSaverController _screenSaverController = null;
    protected SuspendLevel _applicationSuspendLevel = SuspendLevel.None;
    protected SuspendLevel _playerSuspendLevel = SuspendLevel.None;

    /// <summary>
    /// Timespan from the last user input to the start of the screen saver.
    /// </summary>
    protected TimeSpan _screenSaverTimeOut;
    protected bool _mouseHidden = false;
    private readonly object _reclaimDeviceSyncObj = new object();

    private bool _adaptToSizeEnabled;

    public MainForm(ScreenManager screenManager)
    {
      _adaptToSizeEnabled = false;
      _screenManager = screenManager;

      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Registering DirectX MainForm as IScreenControl service");
      ServiceRegistration.Set<IScreenControl>(this);

      InitializeComponent();
      CheckForIllegalCrossThreadCalls = false;

      StartupSettings startupSettings = ServiceRegistration.Get<ISettingsManager>().Load<StartupSettings>();
      AppSettings appSettings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();

      _previousMousePosition = new Point(-1, -1);

      Size desiredWindowedSize = new Size(SkinContext.SkinResources.SkinWidth, SkinContext.SkinResources.SkinHeight);

      // Default screen for splashscreen is the one from where MP2 was started.
      System.Windows.Forms.Screen preferredScreen = System.Windows.Forms.Screen.FromControl(this);
      int numberOfScreens = System.Windows.Forms.Screen.AllScreens.Length;
      int validScreenNum = GetScreenNum();

      // Force the splashscreen to be displayed on a specific screen.
      if (startupSettings.StartupScreenNum >= 0 && startupSettings.StartupScreenNum < numberOfScreens)
      {
        validScreenNum = startupSettings.StartupScreenNum;
        preferredScreen = System.Windows.Forms.Screen.AllScreens[validScreenNum];
        StartPosition = FormStartPosition.Manual;
      }

      Location = new Point(preferredScreen.WorkingArea.X, preferredScreen.WorkingArea.Y);

      _previousWindowLocation = Location;
      _previousWindowClientSize = desiredWindowedSize;
      _previousWindowState = FormWindowState.Normal;

      if (appSettings.FullScreen)
        SwitchToFullscreen(validScreenNum);
      else
        SwitchToWindowedSize(Location, desiredWindowedSize, false);

      SkinContext.WindowSize = ClientSize;

      // GraphicsDevice has to be initialized after the form was sized correctly
      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Initialize DirectX");
      GraphicsDevice.Initialize_MainThread(this);

      // Read and apply ScreenSaver settings
      _screenSaverTimeOut = TimeSpan.FromMinutes(appSettings.ScreenSaverTimeoutMin);
      _isScreenSaverEnabled = appSettings.ScreenSaverEnabled;

      _applicationSuspendLevel = appSettings.SuspendLevel;
      UpdateSystemSuspendLevel_MainThread(); // Don't use UpdateSystemSuspendLevel() here because the window handle was not created yet

      Application.Idle += OnApplicationIdle;
      _adaptToSizeEnabled = true;

      VideoPlayerSynchronizationStrategy = new SynchronizeToPrimaryPlayer();
    }

    /// <summary>
    /// Updates the local state corresponding to the current video player, given in parameter <paramref name="videoPlayer"/>.
    /// This method will check if the given player is suspended (i.e. it is paused). It will also update the thread state so that the system
    /// won't shut down while playing a video.
    /// </summary>
    /// <param name="videoPlayer">Player to check.</param>
    private void UpdateVideoPlayerState(IVideoPlayer videoPlayer)
    {
      IMediaPlaybackControl player = videoPlayer as IMediaPlaybackControl;
      _videoPlayerSuspended = player == null || player.IsPaused;
      PlayerSuspendLevel = videoPlayer == null ? SuspendLevel.None : SuspendLevel.DisplayRequired;
    }

    public SuspendLevel PlayerSuspendLevel
    {
      get { return _playerSuspendLevel; }
      set
      {
        if (_playerSuspendLevel == value)
          return;
        _playerSuspendLevel = value;
        UpdateSystemSuspendLevel();
      }
    }

    protected void ExecuteInMainThread(Dlgt method)
    {
      Invoke(method);
    }

    protected void UpdateSystemSuspendLevel()
    {
      ExecuteInMainThread(UpdateSystemSuspendLevel_MainThread);
    }

    protected void UpdateSystemSuspendLevel_MainThread()
    {
      // We'll use the maximum suspend level from main suspend level and player suspend level
      SuspendLevel value = _applicationSuspendLevel;
      _applicationSuspendLevel = value;
      if (_playerSuspendLevel > value)
        value = _playerSuspendLevel;
      EnergySavingConfig.SetCurrentSuspendLevel(value, true);
    }

    /// <summary>
    /// Returns the information if an EVR player is used to synchronize our render thread.
    /// </summary>
    public bool SynchronizedToEVR
    {
      get { return _synchronizedVideoPlayer != null; }
    }

    protected int GetScreenNum()
    {
      return Array.IndexOf(System.Windows.Forms.Screen.AllScreens, System.Windows.Forms.Screen.FromControl(this));
    }

    protected void SwitchToFullscreen(int screenNum)
    {
      System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
      System.Windows.Forms.Screen screen = screenNum < 0 || screenNum >= screens.Length ?
          System.Windows.Forms.Screen.PrimaryScreen :
          System.Windows.Forms.Screen.AllScreens[screenNum];
      WindowState = FormWindowState.Normal;
      Rectangle rect = screen.Bounds;
      Location = rect.Location;
      ClientSize = rect.Size;
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
      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Dispose DirectX");
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
      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Stopping UI");
      StopRenderThread();
    }

    public void StartUI()
    {
      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Starting UI");
      GraphicsDevice.Reset();
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
      if (SkinContext.RenderThread != null)
        throw new Exception("DirectX MainForm: Render thread already running");
      _renderThreadStopped = false;
      SkinContext.RenderThread = new Thread(RenderLoop) { Name = "DX Render" };
      SkinContext.RenderThread.Start();
    }

    internal void StopRenderThread()
    {
      _renderThreadStopped = true;
      if (SkinContext.RenderThread == null)
        return;
      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Stoping render thread");
      SkinContext.RenderThread.Join();
      SkinContext.RenderThread = null;
    }

    private void SynchronizeToVideoPlayerFramerate(IVideoPlayer videoPlayer)
    {
      lock (_screenManager.SyncObj)
      {
        if (videoPlayer == _synchronizedVideoPlayer)
          return;
        ISlimDXVideoPlayer oldPlayer = _synchronizedVideoPlayer;
        _synchronizedVideoPlayer = null;
        if (oldPlayer != null)
          oldPlayer.SetRenderDelegate(null);
        ISlimDXVideoPlayer slimDxVideoPlayer = videoPlayer as ISlimDXVideoPlayer;
        if (slimDxVideoPlayer != null)
          if (slimDxVideoPlayer.SetRenderDelegate(VideoPlayerRender))
          {
            _synchronizedVideoPlayer = slimDxVideoPlayer;
            ServiceRegistration.Get<ILogger>().Info("SkinEngine MainForm: Synchronized render framerate to video player '{0}'", slimDxVideoPlayer);
          }
          else
            ServiceRegistration.Get<ILogger>().Info(
                "SkinEngine MainForm: Video player '{0}' doesn't provide render thread synchronization, using default framerate", slimDxVideoPlayer);
      }
    }

    private void VideoPlayerRender()
    {
      _videoRenderFrameEvent.Set();
      _renderFinishedEvent.WaitOne(VIDEO_PLAYER_MAX_WAIT_FOR_RENDER_MS);
    }

    /// <summary>
    /// Render loop executed by the default render thread.
    /// </summary>
    private void RenderLoop()
    {
      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Starting main render loop");
      GraphicsDevice.SetRenderState();
      while (!_renderThreadStopped)
      {
        // EVR handling
        bool isVideoPlayer = SynchronizedToEVR;

        _renderFinishedEvent.Reset();

        if (isVideoPlayer && !_videoPlayerSuspended)
          // If our video player synchronizes the rendering, it sets the _videoRenderFrameEvent when a new frame is available,
          // so we wait for that event here.
          _videoRenderFrameEvent.WaitOne(RENDER_MAX_WAIT_FOR_VIDEO_FRAME_MS);

        bool shouldWait = GraphicsDevice.Render(!isVideoPlayer || _videoPlayerSuspended); // If the video player isn't active or if it is suspended, use the configured target framerate of the GraphicsDevice
        _renderFinishedEvent.Set();

        if (shouldWait || !_hasFocus)
          // The device was lost or we don't have focus - reduce the render rate
          Thread.Sleep(10);

        if (!GraphicsDevice.DeviceOk)
          break;
      }
      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Main render loop was stopped");
    }

    public void Start()
    {
      Activate();
      CheckTopMost();
      StartUI();
      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Running");
    }

    public void Shutdown()
    {
      VideoPlayerSynchronizationStrategy = null; // Stops the strategy component
      Close();
      _videoRenderFrameEvent.Close();
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

    public void ConfigureScreenSaver(bool screenSaverEnabled, double screenSaverTimeoutMin)
    {
      AppSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();
      settings.ScreenSaverTimeoutMin = screenSaverTimeoutMin;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
      _screenSaverTimeOut = TimeSpan.FromMinutes(screenSaverTimeoutMin);
      _isScreenSaverEnabled = screenSaverEnabled;
    }

    public ScreenSaverController GetScreenSaverController()
    {
      if (_screenSaverController != null)
      {
        ServiceRegistration.Get<ILogger>().Warn("SkinEngine MainForm: ScreenSaverControl is already registered, prevent creation of another ScreenSaverControl");
        return null;
      }
      return _screenSaverController = new ScreenSaverController(() => { _screenSaverController = null; });
    }

    public void SwitchMode(ScreenMode mode)
    {
      if (InvokeRequired)
      {
        Invoke(new SwitchModeDelegate(SwitchMode), mode);
        return;
      }

      ServiceRegistration.Get<ILogger>().Debug("SkinEngine MainForm: Switching mode to {0}", mode);
      bool newFullscreen = mode == ScreenMode.FullScreen;
      AppSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();

      // Already done, no need to do it twice.
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
          SwitchToFullscreen(GetScreenNum());
        }
        else
          SwitchToWindowedSize(_previousWindowLocation, _previousWindowClientSize, _previousWindowState == FormWindowState.Maximized);
      }
      finally
      {
        _adaptToSizeEnabled = true;
      }
      SkinContext.WindowSize = ClientSize;

      Update();
      Activate();
      CheckTopMost();

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
    }

    public double ScreenSaverTimeoutMin
    {
      get { return _screenSaverTimeOut.TotalMinutes; }
    }

    public IntPtr MainWindowHandle
    {
      get { return Handle; }
    }

    public SuspendLevel ApplicationSuspendLevel
    {
      get { return _applicationSuspendLevel; }
      set
      {
        if (_applicationSuspendLevel == value)
          return;
        _applicationSuspendLevel = value;
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        AppSettings settings = settingsManager.Load<AppSettings>();
        settings.SuspendLevel = _applicationSuspendLevel;
        settingsManager.Save(settings);
        UpdateSystemSuspendLevel();
      }
    }

    public IVideoPlayerSynchronizationStrategy VideoPlayerSynchronizationStrategy
    {
      get { return _videoPlayerSynchronizationStrategy; }
      set
      {
        if (_videoPlayerSynchronizationStrategy != null)
        {
          _videoPlayerSynchronizationStrategy.Stop();
          _videoPlayerSynchronizationStrategy.UpdateVideoPlayerState -= UpdateVideoPlayerState;
          _videoPlayerSynchronizationStrategy.SynchronizeToVideoPlayerFramerate -= SynchronizeToVideoPlayerFramerate;
        }
        _videoPlayerSynchronizationStrategy = value;
        if (_videoPlayerSynchronizationStrategy != null)
        {
          _videoPlayerSynchronizationStrategy.UpdateVideoPlayerState += UpdateVideoPlayerState;
          _videoPlayerSynchronizationStrategy.SynchronizeToVideoPlayerFramerate += SynchronizeToVideoPlayerFramerate;
          _videoPlayerSynchronizationStrategy.Start();
        }
      }
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
        // Remember old state, calls to IScreenManager are only required on state changes
        bool wasScreenSaverActive = _isScreenSaverActive;
        if (_isScreenSaverEnabled)
        {
          IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
          IPlayer primaryPlayer = playerContextManager[PlayerContextIndex.PRIMARY];
          IMediaPlaybackControl mbc = primaryPlayer as IMediaPlaybackControl;
          bool preventScreenSaver = ((primaryPlayer is IVideoPlayer || primaryPlayer is IImagePlayer) && (mbc == null || !mbc.IsPaused)) ||
              playerContextManager.IsFullscreenContentWorkflowStateActive;

          _isScreenSaverActive = !preventScreenSaver &&
              SkinContext.FrameRenderingStartTime - inputManager.LastMouseUsageTime > _screenSaverTimeOut &&
              SkinContext.FrameRenderingStartTime - inputManager.LastInputTime > _screenSaverTimeOut;

          if (_screenSaverController != null)
          {
            bool? activeOverride = _screenSaverController.IsScreenSaverActiveOverride;
            if (activeOverride.HasValue)
              _isScreenSaverActive = activeOverride.Value;
          }
        }
        else
          _isScreenSaverActive = false;
        if (wasScreenSaverActive != _isScreenSaverActive)
        {
          IScreenManager superLayerManager = ServiceRegistration.Get<IScreenManager>();
          superLayerManager.SetSuperLayer(_isScreenSaverActive ? SCREEN_SAVER_SCREEN : null);
        }

        // If we are in fullscreen mode, we may control the mouse cursor, else reset it to visible state, if state was switched
        ShowMouseCursor(!IsFullScreen || inputManager.IsMouseUsed);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SkinEngine MainForm: Error occured in Idle handler", ex);
      }
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      try
      {
        logger.Debug("SkinEngine MainForm: Stopping");
        StopUI();
        UIResourcesHelper.ReleaseUIResources();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SkinEngine MainForm: Error occured in FormClosing handler", ex);
      }
      logger.Debug("SkinEngine MainForm: Closing");
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
        ServiceRegistration.Get<ILogger>().Error("SkinEngine MainForm: Error occured in MouseMove handler", ex);
      }
    }

    private static void MainForm_KeyDown(object sender, KeyEventArgs e)
    {
      try
      {
        // We'll handle special keys here
        Key key = InputMapper.MapSpecialKey(e);
        if (key != Key.None && key != Key.Close)
        {
          IInputManager manager = ServiceRegistration.Get<IInputManager>();
          manager.KeyPress(key);
          e.Handled = true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SkinEngine MainForm: Error occured in KeyDown handler", ex);
      }
    }

    private static void MainForm_KeyPress(object sender, KeyPressEventArgs e)
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
        ServiceRegistration.Get<ILogger>().Error("SkinEngine MainForm: Error occured in KeyPress handler", ex);
      }
    }

    private static void MainForm_MouseClick(object sender, MouseEventArgs e)
    {
      try
      {
        IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
        inputManager.MouseClick(e.Button);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SkinEngine MainForm: Error occured in MouseClick handler", ex);
      }
    }

    private void timer_Tick(object sender, EventArgs e)
    {
      // Avoid multiple threads in here.
      if (Monitor.TryEnter(_reclaimDeviceSyncObj))
        try
        {
          if (!GraphicsDevice.DeviceOk)
          {
            StopRenderThread();
            if (_hasFocus)
            {
              if (GraphicsDevice.ReclaimDevice())
                StartRenderThread_Async();
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
      // Don't react on window state changes, those are captured by OnSizeChanged()
      if (_adaptToSizeEnabled && ClientSize != _previousWindowClientSize && _previousWindowState == WindowState)
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
