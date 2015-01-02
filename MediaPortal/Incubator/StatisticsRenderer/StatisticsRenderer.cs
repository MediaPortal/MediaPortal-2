#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;
using SwapChain = SharpDX.DXGI.SwapChain;

namespace MediaPortal.Plugins.StatisticsRenderer
{
  public class StatisticsRenderer : IPluginStateTracker, IDisposable
  {
    #region Constants and structs

    private const int RENDER_OFFSET_LEFT = 20;
    private const int RENDER_OFFSET_TOP = 200;
    private const int TEXT_SIZE = 20;
    private const int MAX_STAT_VALUES = 500;
    struct Stats
    {
      public float Fps;
      public float MsToNextVBlank;
      public float MsFrameRenderTime;
      public int Glitch;
    }

    #endregion

    #region Fields

    private GraphicsDevice11 _device;
    private SwapChain _swapChain;
    private ModeDescription _adapterDisplayModeEx;

    private readonly RingBuffer<Stats> _stats = new RingBuffer<Stats>(MAX_STAT_VALUES, true);

    private int _tearingPos;
    private TextFormat _textFormat;

    private TimeSpan _totalGuiRenderDuration;
    private TimeSpan _guiRenderDuration;
    private int _totalFrameCount = 0;
    private int _frameCount = 0;
    private float _sumMsToVBlank = 0;
    private DateTime _frameRenderingStartTime;
    private int _fpsCounter;
    private DateTime _fpsTimer;
    private string _perfLogString;
    private volatile bool _statsEnabled;

    private float _fps;
    protected ManualResetEvent _renderFinished = new ManualResetEvent(true);

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();

    private SolidColorBrush _whiteBrush;
    private SolidColorBrush _grayBrush;
    private SolidColorBrush _redBrush;
    private SolidColorBrush _greenBrush;
    private SolidColorBrush _blueBrush;
    private SolidColorBrush _yellowBrush;

    #endregion

    #region Members

    /// <summary>
    /// Allocates DirectX resources and attaches to render events.
    /// </summary>
    private void EnableStats()
    {
      Log("Try get Device");
      _device = SkinContext.Device11;
      if (_device.Device2D1 == null)
        return;

      _swapChain = _device.SwapChain;

      _whiteBrush = new SolidColorBrush(_device.Context2D1, Color.White);
      _grayBrush = new SolidColorBrush(_device.Context2D1, Color.Gray);
      _redBrush = new SolidColorBrush(_device.Context2D1, Color.Red);
      _greenBrush = new SolidColorBrush(_device.Context2D1, Color.Gray);
      _blueBrush = new SolidColorBrush(_device.Context2D1, Color.Blue);
      _yellowBrush = new SolidColorBrush(_device.Context2D1, Color.Yellow);

      _textFormat = new TextFormat(_device.FactoryDW, "Tahoma", FontWeight.Normal, FontStyle.Normal, TEXT_SIZE);

      // Get device info
      _adapterDisplayModeEx = SkinContext.Device11.SwapChain.Description.ModeDescription;
      Log("Screen height {0} at refresh rate {1} Hz", _adapterDisplayModeEx.Height, _adapterDisplayModeEx.RefreshRate);

      Log("Attach render events");
      SkinContext.DeviceSceneBegin += BeginScene;
      SkinContext.DeviceSceneEnd += EndScene;
      _statsEnabled = true;
    }

    private static void TryDispose<TE>(ref TE disposable)
    {
      IDisposable disp = disposable as IDisposable;
      if (disp != null)
        disp.Dispose();
      disposable = default(TE);
    }

    /// <summary>
    /// Detaches renderer events and frees DirectX resources.
    /// </summary>
    private void DisableStats()
    {
      _statsEnabled = false; // Avoids new stats render cycles after our waiting for the render event
      SkinContext.DeviceSceneBegin -= BeginScene;
      SkinContext.DeviceSceneEnd -= EndScene;
      _renderFinished.WaitOne(TimeSpan.FromSeconds(2)); // Wait for the last render cycle to finish
      TryDispose(ref _whiteBrush);
      TryDispose(ref _grayBrush);
      TryDispose(ref _redBrush);
      TryDispose(ref _greenBrush);
      TryDispose(ref _blueBrush);
      TryDispose(ref _yellowBrush);
      TryDispose(ref _textFormat);
    }

    /// <summary>
    /// ToggleRenderMode calls the <see cref="SkinContext.NextRenderStrategy"/> method to switch between the available RenderModes.
    /// </summary>
    private static void ToggleRenderMode()
    {
      Log("Toggling render mode...");
      SkinContext.NextRenderStrategy();
      Log("Render mode is now '" + SkinContext.RenderStrategy.Name + "'");
    }

    /// <summary>
    /// ToggleRenderPipeline calls the <see cref="SkinContext.NextRenderPipeline"/> method to switch between the available RenderPipelines.
    /// </summary>
    private static void ToggleRenderPipeline()
    {
      Log("Toggling render pipeline...");
      SkinContext.NextRenderPipeline();
      Log("Render pipeline is now '" + SkinContext.RenderPipeline.GetType().Name + "'");
    }

    /// <summary>
    /// ToggleStatsRendering enables or disables the stats gathering and rendering.
    /// </summary>
    private void ToggleStatsRendering()
    {
      if (_statsEnabled)
      {
        Log("Disabling render statistics rendering");
        DisableStats();
      }
      else
      {
        Log("Enabling render statistics rendering");
        EnableStats();
      }
    }

    #endregion

    #region Render event handlers

    public void BeginScene(object sender, EventArgs args)
    {
      if (!_statsEnabled)
        return;

      _frameRenderingStartTime = DateTime.Now;
    }

    public void EndScene(object sender, EventArgs args)
    {
      if (!_statsEnabled)
        return;
      _renderFinished.Reset();
      try
      {
        TimeSpan guiDur = DateTime.Now - _frameRenderingStartTime;
        _totalGuiRenderDuration += guiDur;
        _guiRenderDuration += guiDur;
        _totalFrameCount++;
        _frameCount++;
        int scanLine = 0; // Not supported in D3D11? // _device.GetRasterStatus(0).ScanLine;
        _sumMsToVBlank += ScanlineToVblankOffset(scanLine);

        _fpsCounter++;
        TimeSpan ts = DateTime.Now - _fpsTimer;
        float secs = (float)ts.TotalSeconds;
        _fps = _fpsCounter / secs;

        Stats currentFrameStats = new Stats
          {
            Fps = _fps,
            MsFrameRenderTime = (float)guiDur.TotalMilliseconds,
            MsToNextVBlank = ScanlineToVblankOffset(scanLine)
          };

        if (ts.TotalSeconds >= 1.0f)
        {
          float totalAvgGuiTime = (float)_totalGuiRenderDuration.TotalMilliseconds / _totalFrameCount;
          float avgGuiTime = (float)_guiRenderDuration.TotalMilliseconds / _frameCount;
          float avgMsToVBlank = _sumMsToVBlank / _frameCount;

          int glitches;
          _perfLogString = string.Format(
              "Render: {0:0.00} fps [red], {1} frames last meas., avg GUI time {2:0.00}, last sec: {3:0.00} [blue], " +
              "avg ms to vblank: {6:0.00} [grn]\r\nRender mode enabled: {4}\r\n{5}",
              _fps, _fpsCounter, totalAvgGuiTime, avgGuiTime, SkinContext.RenderStrategy.Name,
              GetPresentStats(out glitches), avgMsToVBlank);

          currentFrameStats.Glitch = glitches;
          _perfLogString += "\r\n" + GetScreenInfo();
          _perfLogString += "\r\n" + GetRenderPipelineInfo();
          _perfLogString += "\r\n" + GetPlayerInfos();
          _fpsCounter = 0;
          _frameCount = 0;
          _guiRenderDuration = TimeSpan.Zero;
          _fpsTimer = DateTime.Now;
          _sumMsToVBlank = 0;
        }

        _stats.Push(currentFrameStats);

        _device.Context2D1.BeginDraw();
        DrawLines();
        DrawTearingTest();
        DrawText(_perfLogString);
        _device.Context2D1.EndDraw();
      }
      finally
      {
        _renderFinished.Set();
      }
    }

    #endregion

    #region Rendering

    private void DrawTearingTest()
    {
      int left = _tearingPos;
      int width = _device.Context2D1.PixelSize.Width;
      int height = _device.Context2D1.PixelSize.Height;
      Size size = new Size(4, height);
      Point topLeft = new Point(left, 0);
      if (topLeft.X + size.Width >= width)
        topLeft.X = 0;

      Rectangle rcTearing = SharpDXExtensions.CreateRectangle(topLeft, size);

      _device.Context2D1.DrawRectangle(rcTearing, _whiteBrush);

      topLeft = new Point((rcTearing.Right + 15) % width, 0);
      if (topLeft.X + size.Width >= width)
        topLeft.X = 0;

      rcTearing = SharpDXExtensions.CreateRectangle(topLeft, size);
      _device.Context2D1.DrawRectangle(rcTearing, _grayBrush);

      _tearingPos = (_tearingPos + 7) % width;
    }


    private void DrawText(string text)
    {
      using (var layout = new TextLayout(_device.FactoryDW, text, _textFormat, SkinContext.BackBufferWidth, SkinContext.BackBufferHeight))
      {

        Rectangle rcTextField = new Rectangle(RENDER_OFFSET_LEFT, 0, SkinContext.BackBufferWidth - RENDER_OFFSET_LEFT, (int)Math.Ceiling(layout.Metrics.Height));
        _device.Context2D1.DrawText(text, _textFormat, rcTextField, _redBrush);
      }
    }

    private void DrawLines()
    {
      Vector2[] pointsFps = new Vector2[2];
      Vector2[] pointsRenderTime = new Vector2[2];
      Vector2[] pointsTimeToPresent = new Vector2[2];
      Vector2[] pointsGlitches = new Vector2[2];
      Vector2[] renderBaseLine = new Vector2[2];
      Vector2[] presentBaseLine = new Vector2[2];
      renderBaseLine[0].X = presentBaseLine[0].X = RENDER_OFFSET_LEFT;
      renderBaseLine[1].X = presentBaseLine[1].X = RENDER_OFFSET_LEFT + MAX_STAT_VALUES;
      renderBaseLine[0].Y = renderBaseLine[1].Y = RENDER_OFFSET_TOP + 30;
      presentBaseLine[0].Y = presentBaseLine[1].Y = RENDER_OFFSET_TOP + 70;
      int x = 0;

      float strokeWidth = 2.5f;
      _device.Context2D1.DrawLine(renderBaseLine[0], renderBaseLine[1], _grayBrush, strokeWidth);
      _device.Context2D1.DrawLine(presentBaseLine[0], presentBaseLine[1], _grayBrush, strokeWidth);

      foreach (Stats stats in _stats.ReadAll(_totalFrameCount))
      {
        int pIdx = x % 2;
        pointsFps[pIdx].X = pointsRenderTime[pIdx].X = pointsTimeToPresent[pIdx].X = pointsGlitches[pIdx].X = RENDER_OFFSET_LEFT + x;

        pointsFps[pIdx].Y = RENDER_OFFSET_TOP - stats.Fps;
        pointsRenderTime[pIdx].Y = RENDER_OFFSET_TOP + 30 + 2 * stats.MsFrameRenderTime;
        pointsTimeToPresent[pIdx].Y = RENDER_OFFSET_TOP + 70 - 2 * stats.MsToNextVBlank;
        pointsGlitches[pIdx].Y = RENDER_OFFSET_TOP + stats.Glitch * -5; // scale to see glitches better, mirror them
        if (pIdx == 1)
        {
          _device.Context2D1.DrawLine(pointsFps[0], pointsFps[1], _redBrush, strokeWidth);
          _device.Context2D1.DrawLine(pointsRenderTime[0], pointsRenderTime[1], _blueBrush, strokeWidth);
          _device.Context2D1.DrawLine(pointsTimeToPresent[0], pointsTimeToPresent[1], _greenBrush, strokeWidth);
          _device.Context2D1.DrawLine(pointsGlitches[0], pointsGlitches[1], _yellowBrush, strokeWidth);
        }
        x++;
      }
    }

    #endregion

    #region Helpers

    private float ScanlineToVblankOffset(int currentScanline)
    {
      float singleLineDuration = 1000f / ((float)_adapterDisplayModeEx.RefreshRate.Numerator / _adapterDisplayModeEx.RefreshRate.Denominator) / _adapterDisplayModeEx.Height;
      return (_adapterDisplayModeEx.Height - currentScanline) * singleLineDuration;
    }

    private String GetPresentStats(out int glitchCount)
    {
      string presentStats = string.Empty;
      glitchCount = 0;
      if (SkinContext.Device11.SwapChain.Description.SwapEffect != SwapEffect.FlipSequential)
        return "Present statistics only available when using SwapEffect.FlipSequential.";

      try
      {
        var stats = _swapChain.FrameStatistics;
        glitchCount = stats.SyncRefreshCount - stats.PresentRefreshCount;
        int presentCount = stats.PresentCount;
        presentStats = string.Format("PresentCount: {0}, Glitches (SRC-PRC): {1} [ylw]", presentCount, glitchCount);
      }
      catch (SharpDXException e)
      {
        // Ignore "stats disjoint" exception that can happen the first time access of FrameStatistics
        if (e.ResultCode.Code != -2005270517)
          throw;
      }
      return presentStats;
    }

    private string GetScreenInfo()
    {
      return string.Format("Screen Res: {0}x{1} @ {2}Hz", SkinContext.BackBufferWidth, SkinContext.BackBufferHeight, _adapterDisplayModeEx.RefreshRate);
    }

    private string GetRenderPipelineInfo()
    {
      return string.Format("Render Pipeline: {0}", SkinContext.RenderPipeline.GetType().Name);
    }

    private static string GetPlayerInfos()
    {
      string playerInfos = string.Empty;
      IPlayerManager pm = ServiceRegistration.Get<IPlayerManager>();
      int index = 0;
      pm.ForEach(psc =>
        {
          ISharpDXVideoPlayer player = psc.CurrentPlayer as ISharpDXVideoPlayer;
          if (player == null || player.Surface == null)
            return;
          var desc = player.Surface.Bitmap.PixelSize;
          playerInfos += String.Format("{0}Player {1}: Resolution {2}x{3}", string.IsNullOrEmpty(playerInfos) ? "" : "\r\n", index++, desc.Width, desc.Height);
        });
      return playerInfos;
    }

    private static void Log(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Debug("StatisticsRenderer: " + format, args);
    }

    #endregion

    #region IDisposable implementation

    /// <summary>
    /// Free resources.
    /// </summary>
    public void Dispose()
    {
      DisableStats();
    }

    #endregion

    private void RegisterKeyBindings()
    {
      IInputManager manager = ServiceRegistration.Get<IInputManager>(false);
      if (manager != null)
      {
        Log("Registering KeyBindings on IInputManager");
        manager.AddKeyBinding(Key.F10, new VoidKeyActionDlgt(ToggleStatsRendering));
        manager.AddKeyBinding(Key.F11, new VoidKeyActionDlgt(ToggleRenderMode));
        manager.AddKeyBinding(Key.F12, new VoidKeyActionDlgt(ToggleRenderPipeline));
      }
    }

    private static void UnregisterKeyBindings()
    {
      Log("Registering KeyBindings on IInputManager");
      IInputManager manager = ServiceRegistration.Get<IInputManager>(false);
      if (manager != null)
      {
        manager.RemoveKeyBinding(Key.F10);
        manager.RemoveKeyBinding(Key.F11);
        manager.RemoveKeyBinding(Key.F12);
      }
    }

    protected void DropMessageQueue()
    {
      lock (_syncObj)
      {
        if (_messageQueue != null)
          _messageQueue.Terminate();
        _messageQueue = null;
      }
    }

    public void Activated(PluginRuntime pluginRuntime)
    {
      ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
      if (sss.CurrentState == SystemState.Running)
      {
        RegisterKeyBindings();
      }
      else
      {
        _messageQueue = new AsynchronousMessageQueue(typeof(StatisticsRenderer), new string[]
          {
              SystemMessaging.CHANNEL
          });
        _messageQueue.MessageReceived += OnMessageReceived;
        _messageQueue.Start();
      }
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
          if (newState == SystemState.Running)
          {
            RegisterKeyBindings();
          }
          if (newState == SystemState.ShuttingDown)
          {
            DisableStats();
            UnregisterKeyBindings();
            DropMessageQueue();
          }
        }
      }
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
      DisableStats();
      UnregisterKeyBindings();
      DropMessageQueue();
    }
  }
}
