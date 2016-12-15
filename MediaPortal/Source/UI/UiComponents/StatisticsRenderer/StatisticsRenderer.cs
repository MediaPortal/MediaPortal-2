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
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

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

    private DeviceEx _device;
    private SwapChain _swapChain;
    private SwapChain9Ex _swapChainEx;
    private DisplayModeEx _adapterDisplayModeEx;

    private readonly RingBuffer<Stats> _stats = new RingBuffer<Stats>(MAX_STAT_VALUES, true);

    private int _tearingPos;
    private Sprite _fontSprite;
    private Font _font;
    private Line _line;

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

    #endregion

    #region Members

    /// <summary>
    /// Allocates DirectX resources and attaches to render events.
    /// </summary>
    private void EnableStats()
    {
      Log("Try get Device");
      _device = SkinContext.Device;
      if (_device == null)
        return;

      _swapChain = _device.GetSwapChain(0);
      _swapChainEx = new SwapChain9Ex(_swapChain.NativePointer);
      _line = new Line(_device) { Width = 2.5f, Antialias = true };

      _fontSprite = new Sprite(_device);
      _font = new Font(_device, TEXT_SIZE, 0, FontWeight.Normal, 0, false, FontCharacterSet.Default, FontPrecision.Default, FontQuality.ClearTypeNatural, FontPitchAndFamily.DontCare, "tahoma");

      // Get device info
      _adapterDisplayModeEx = SkinContext.Direct3D.GetAdapterDisplayModeEx(0);
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
      TryDispose(ref _line);
      TryDispose(ref _font);
      TryDispose(ref _fontSprite);
      TryDispose(ref _swapChainEx);
      TryDispose(ref _swapChain);
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
        int scanLine = _device.GetRasterStatus(0).ScanLine;
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
        DrawLines();
        DrawTearingTest();
        DrawText(_perfLogString);
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
      using (Surface surface = _device.GetRenderTarget(0))
      {
        int left = _tearingPos;
        int width = surface.Description.Width;
        int height = surface.Description.Height;
        Size size = new Size(4, height);
        Point topLeft = new Point(left, 0);
        if (topLeft.X + size.Width >= width)
          topLeft.X = 0;

        Rectangle rcTearing = SharpDXExtensions.CreateRectangle(topLeft, size);

        _device.ColorFill(surface, rcTearing, ColorConverter.FromArgb(255, 255, 255, 255));

        topLeft = new Point((rcTearing.Right + 15) % width, 0);
        if (topLeft.X + size.Width >= width)
          topLeft.X = 0;

        rcTearing = SharpDXExtensions.CreateRectangle(topLeft, size);
        _device.ColorFill(surface, rcTearing, ColorConverter.FromArgb(255, 100, 100, 100));

        _tearingPos = (_tearingPos + 7) % width;
      }
    }

    private void DrawText(string text)
    {
      int numberOfLines = text.Split('\n').Length;
      int bgHeight = TEXT_SIZE * numberOfLines;
      Rectangle rcTextField = new Rectangle(RENDER_OFFSET_LEFT, 0, SkinContext.BackBufferWidth - RENDER_OFFSET_LEFT, bgHeight);
      _fontSprite.Begin(SpriteFlags.AlphaBlend);
      _font.DrawText(_fontSprite, text, rcTextField, FontDrawFlags.Left, Color.Red);
      _fontSprite.End();
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
      _line.Begin();

      _line.Draw(renderBaseLine, new ColorBGRA(0.8f, 0.8f, 0.8f, 0.8f));
      _line.Draw(presentBaseLine, new ColorBGRA(0.8f, 0.8f, 0.8f, 0.8f));

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
          _line.Draw(pointsFps, Color.Red);
          _line.Draw(pointsRenderTime, Color.Blue);
          _line.Draw(pointsTimeToPresent, Color.Green);
          _line.Draw(pointsGlitches, Color.Yellow);
        }
        x++;
      }
      _line.End();
    }

    #endregion

    #region Helpers

    private float ScanlineToVblankOffset(int currentScanline)
    {
      float singleLineDuration = 1000f / _adapterDisplayModeEx.RefreshRate / _adapterDisplayModeEx.Height;
      return (_adapterDisplayModeEx.Height - currentScanline) * singleLineDuration;
    }

    private String GetPresentStats(out int glitchCount)
    {
      string presentStats = string.Empty;
      glitchCount = 0;
      if (SkinContext.IsMultiSample)
        return "Present statistics only available when not using antialiasing.";

      try
      {
        PresentationStatistics stats = _swapChainEx.PresentStats;
        glitchCount = stats.SyncRefreshCount - stats.PresentRefreshCount;
        presentStats = string.Format("PresentCount: {0}, Glitches (SRC-PRC): {1} [ylw]", stats.PresentCount, glitchCount);
      }
      catch (SharpDXException e)
      {
        // Ignore "stats disjoint" exception that can happen the first time access of PresentStatistics
        if (e.ResultCode.Code != -2005530492)
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
          if (player == null || player.Texture == null)
            return;
          SurfaceDescription desc = player.Texture.GetLevelDescription(0);
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
