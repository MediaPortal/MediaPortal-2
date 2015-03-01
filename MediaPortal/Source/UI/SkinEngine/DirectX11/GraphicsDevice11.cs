#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
#define PROFILE_PERFORMANCE_MODE
#region Usings
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.RenderPipelines;
using MediaPortal.UI.SkinEngine.DirectX.RenderStrategy;
using MediaPortal.UI.SkinEngine.Fonts;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Settings;
using MediaPortal.UI.SkinEngine.Utils;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.WIC;
using Device = SharpDX.Direct3D11.Device;
using Device1 = SharpDX.Direct3D11.Device1;
using DeviceContext = SharpDX.Direct2D1.DeviceContext;
using Factory = SharpDX.DirectWrite.Factory;
using Factory1 = SharpDX.Direct2D1.Factory1;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;
using Format = SharpDX.DXGI.Format;
using InterpolationMode = SharpDX.Direct2D1.InterpolationMode;
using SwapChain = SharpDX.DXGI.SwapChain;
using SwapEffect = SharpDX.DXGI.SwapEffect;
using Usage = SharpDX.DXGI.Usage;
#endregion

namespace MediaPortal.UI.SkinEngine.DirectX11
{
  public class GraphicsDevice11 : IDisposable
  {
    #region Fields

    private Device _device3D;
    private Device1 _device3D1;
    private SharpDX.DXGI.Device _deviceDXGI;
    private SharpDX.Direct2D1.Device _device2D1;
    private readonly D3DSetup _setup = new D3DSetup();
    private ScreenManager _screenManager = null;

    private SwapChain _swapChain;
    private Texture2D _backBufferTexture;
    private Surface1 _backBuffer;
    private Factory1 _factory2D;
    private DeviceContext _context2D;
    private Bitmap1 _renderTarget2D;
    private Factory _factoryDW;
    private ImagingFactory2 _factoryWIC;

    // RenderModeType related fields
    private int _currentRenderStrategyIndex = 0;
    private List<IRenderStrategy> _renderStrategies;

    // RenderPipeline related fields
    private int _currentRenderPipeplineIndex;
    private List<IRenderPipeline> _renderPipelines;

    // Render quality / performance related fields
    private InterpolationMode _imageInterpolationMode;
    private InterpolationMode _videoInterpolationMode;

    private readonly ReaderWriterLockSlim _renderAndResourceAccessLock = new ReaderWriterLockSlim();
    private bool _useAntialiasing;

    private bool _resetRequired;

    #endregion

    #region Static properties

    private static GraphicsDevice11 _instance;

    public static GraphicsDevice11 Instance
    {
      get { return _instance ?? (_instance = new GraphicsDevice11()); }
    }

    #endregion

    #region Construction/Destruction

    /// <summary>
    /// Initializes or re-initializes the DirectX device and the backbuffer. This is necessary in the initialization phase
    /// of the SkinEngine and after a parameter was changed which affects the DX device creation.
    /// </summary>
    /// <remarks>
    /// This method has to be called from the main application thread because the DirectX device will be created by this method.
    /// </remarks>
    /// <param name="window">The window which is being used as render target; that window will contain the DX device.</param>
    internal void Initialize_MainThread(Form window)
    {
      RenderTarget = window;
      CreateDevice();
      InitDefaults();
    }

    public void Dispose()
    {
      TryDispose(ref _backBuffer);
      TryDispose(ref _backBufferTexture);
      TryDispose(ref _swapChain);

      TryDispose(ref _factory2D);
      TryDispose(ref _renderTarget2D);
      TryDispose(ref _context2D);

      TryDispose(ref _factoryDW);
      TryDispose(ref _factoryWIC);

      TryDispose(ref _device3D1);
      TryDispose(ref _device3D);
      TryDispose(ref _device2D1);
      TryDispose(ref _deviceDXGI);

      MPF.TryDisposeList(ref _renderPipelines);
    }

    #endregion

    #region Public properties

    public Form RenderTarget { get; internal set; }

    public Surface1 BackBuffer
    {
      get { return _backBuffer; }
    }

    public Texture2D BackBufferTexture
    {
      get { return _backBufferTexture; }
    }

    public SwapChain SwapChain
    {
      get { return _swapChain; }
    }

    public Device Device3D
    {
      get { return _device3D; }
    }

    public Device1 Device3D1
    {
      get { return _device3D1; }
    }

    public SharpDX.DXGI.Device DeviceDXGI
    {
      get { return _deviceDXGI; }
    }

    public SharpDX.Direct2D1.Device Device2D1
    {
      get { return _device2D1; }
    }

    public DeviceContext Context2D1
    {
      get { return _context2D; }
    }

    public Bitmap1 RenderTarget2D
    {
      get { return _renderTarget2D; }
    }

    public Factory1 Factory2D
    {
      get { return _factory2D; }
    }

    public Factory FactoryDW
    {
      get { return _factoryDW; }
    }

    public ImagingFactory2 FactoryWIC
    {
      get { return _factoryWIC; }
    }

    public ScreenManager ScreenManager
    {
      get { return _screenManager; }
      internal set { _screenManager = value; }
    }

    public RenderPassType RenderPass { get; set; }

    /// <summary>
    /// Gets the default interpolation mode for drawing images. This property influences quality and performance and should be adjusted for optimal results.
    /// </summary>
    public InterpolationMode ImageInterpolationMode
    {
      get { return _imageInterpolationMode; }
      set
      {
        // Property involves changes to device dependend resources, so recreate them by a device reset
        var reset = _imageInterpolationMode != value;
        _imageInterpolationMode = value;
        if (reset)
          Reset();
      }
    }

    /// <summary>
    /// Gets the default interpolation mode for drawing video frames. This property influences quality and performance and should be adjusted for optimal results.
    /// </summary>
    public InterpolationMode VideoInterpolationMode
    {
      get { return _videoInterpolationMode; }
      set
      {
        // Property involves changes to device dependend resources, so recreate them by a device reset
        var reset = _videoInterpolationMode != value;
        _videoInterpolationMode = value;
        if (reset)
          Reset();
      }
    }

    /// <summary>
    /// Gets or sets if drawing should be antialiased.
    /// </summary>
    public bool UseAntialiasing
    {
      get { return _useAntialiasing; }
      set
      {
        _useAntialiasing = value;
        Context2D1.AntialiasMode = AntialiasMode;
      }
    }

    /// <summary>
    /// Gets the antialiasing mode for Direct2D drawing operations. The value can only be changed by <see cref="UseAntialiasing"/> property.
    /// </summary>
    public AntialiasMode AntialiasMode
    {
      get
      {
        return UseAntialiasing ? AntialiasMode.PerPrimitive : AntialiasMode.Aliased;
      }
    }

    /// <summary>
    /// Gets the current <see cref="IRenderStrategy"/>.
    /// </summary>
    public IRenderStrategy RenderStrategy
    {
      get { return _renderStrategies[_currentRenderStrategyIndex]; }
    }

    /// <summary>
    /// Switches through all possible RenderStrategies.
    /// </summary>
    public void NextRenderStrategy()
    {
      _currentRenderStrategyIndex = (_currentRenderStrategyIndex + 1) % _renderStrategies.Count;
    }

    /// <summary>
    /// Gets the current <see cref="IRenderPipeline"/>.
    /// </summary>
    public IRenderPipeline RenderPipeline
    {
      get { return _renderPipelines[_currentRenderPipeplineIndex]; }
    }

    /// <summary>
    /// Switches through all possible RenderPipelines.
    /// </summary>
    public void NextRenderPipeline()
    {
      _currentRenderPipeplineIndex = (_currentRenderPipeplineIndex + 1) % _renderPipelines.Count;
    }

    #endregion

    #region Public events
    // Render process related events
    public event EventHandler DeviceSceneBegin;
    public event EventHandler DeviceSceneEnd;
    public event EventHandler DeviceScenePresented;
    #endregion

    #region Public methods

    public void ExecuteInMainThread(WorkDlgt method)
    {
      RenderTarget.Invoke(method);
    }

    /// <summary>
    /// Renders the entire scene.
    /// </summary>
    /// <param name="doWaitForNextFame"><c>true</c>, if this method should wait to the correct frame start time
    /// before it renders, else <c>false</c>.</param>
    /// <returns><c>true</c>, if the caller should wait some milliseconds before rendering the next time.</returns>
    public bool Render(bool doWaitForNextFame)
    {
      if (_device2D1 == null)
        return true;

      _renderAndResourceAccessLock.EnterReadLock();

      IRenderStrategy renderStrategy = RenderStrategy;
      IRenderPipeline pipeline = RenderPipeline;

      renderStrategy.BeginRender(doWaitForNextFame);

      try
      {
        CheckReset();

        Fire(DeviceSceneBegin);

        pipeline.BeginRender();

        pipeline.Render();

        pipeline.EndRender();

        Fire(DeviceSceneEnd);

        _swapChain.Present(renderStrategy.SyncInterval, renderStrategy.PresentFlags);

        Fire(DeviceScenePresented);

        ContentManager.Instance.Clean();
      }
      catch (SharpDXException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: DirectX Exception", e);
        HandleDeviceLost(e);
        return false;
      }
      finally
      {
        _renderAndResourceAccessLock.ExitReadLock();
      }
      return false;
    }

    public void HandleDeviceLost(SharpDXException e, bool deferReset = false)
    {
      // D2DERR_RECREATE_TARGET/RecreateTarget
      // DXGI_ERROR_DEVICE_REMOVED/DeviceRemoved
      if (e.ResultCode == 0x8899000C || e.ResultCode == 0x887A0005)
      {
        ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: DeviceRemovedReason: {0}", Device3D1.DeviceRemovedReason);
        if (deferReset)
          _resetRequired = true;
        else
          Reset();
      }
    }

    /// <summary>
    /// Sets a flag the DX device needs to be reset in next render cycle.
    /// </summary>
    public void SetResetRequired()
    {
      _resetRequired = true;
    }

    protected void CheckReset()
    {
      if (_resetRequired)
        Reset();
    }

    /// <summary>
    /// Resets the DirectX device. This will release all screens, other UI resources and our back buffer, reset the DX device and realloc
    /// all resources.
    /// </summary>
    public bool Reset()
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Resetting DX11 device...");
        _screenManager.ExecuteWithTempReleasedResources(() => ExecuteInMainThread(() =>
        {
          ServiceRegistration.Get<ILogger>().Debug("GraphicsDevice: Reset DirectX");
          UIResourcesHelper.ReleaseUIResources();

          if (ContentManager.Instance.TotalAllocationSize != 0)
            ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: ContentManager.TotalAllocationSize = {0}, should be 0!", ContentManager.Instance.TotalAllocationSize / (1024 * 1024));

          Dispose();
          CreateDevice();

          UIResourcesHelper.ReallocUIResources();
        }));
        ServiceRegistration.Get<ILogger>().Warn("GraphicsDevice: Device successfully reset");
        return true;
      }
      finally
      {
        _resetRequired = false;
      }
    }

    #endregion

    #region Private methods

    private void InitDefaults()
    {
      // Init some performance / quality relevant properties
      var appSettings = ServiceRegistration.Get<ISettingsManager>().Load<AppSettings>();
      _imageInterpolationMode = appSettings.ImageInterpolationMode;
      _videoInterpolationMode = appSettings.VideoInterpolationMode;
      UseAntialiasing = appSettings.UseAntialiasing;
    }

    private void CreateDevice()
    {
      // SwapChain description
      int width = RenderTarget.ClientSize.Width;
      int height = RenderTarget.ClientSize.Height;
      var desc = new SwapChainDescription
      {
#if PROFILE_PERFORMANCE_MODE
        BufferCount = 1,
        SwapEffect = SwapEffect.Discard,
#else
        BufferCount = 4,
        SwapEffect = SwapEffect.FlipSequential,
#endif
        ModeDescription = new ModeDescription(width, height, new Rational(50, 1), Format.R8G8B8A8_UNorm),
        IsWindowed = true,
        OutputHandle = RenderTarget.Handle,
        SampleDescription = new SampleDescription(1, 0),
        Usage = Usage.RenderTargetOutput
      };

      // Create Device and SwapChain
      var flags = DeviceCreationFlags.VideoSupport | DeviceCreationFlags.BgraSupport;
      FeatureLevel[] featureLevels =
      {
        FeatureLevel.Level_9_1,
        FeatureLevel.Level_9_2,
        FeatureLevel.Level_9_3,
        FeatureLevel.Level_10_0,
        FeatureLevel.Level_10_1,
        FeatureLevel.Level_11_0,
        FeatureLevel.Level_11_1
      };

      Device.CreateWithSwapChain(DriverType.Hardware, flags, featureLevels, desc, out _device3D, out _swapChain);

      // New RenderTargetView from the backbuffer
      _backBufferTexture = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
      _backBuffer = _backBufferTexture.QueryInterface<Surface1>();

      _device3D1 = _device3D.QueryInterface<Device1>(); // get a reference to the Direct3D 11.1 device
      _deviceDXGI = _device3D1.QueryInterface<SharpDX.DXGI.Device>(); // get a reference to DXGI device

      _factory2D = new Factory1();
      _device2D1 = new SharpDX.Direct2D1.Device(_factory2D, _deviceDXGI); // initialize the D2D device

      _context2D = new DeviceContext(_device2D1, DeviceContextOptions.EnableMultithreadedOptimizations);

      _renderTarget2D = new Bitmap1(_context2D, _backBuffer);
      _context2D.Target = _renderTarget2D;

      _factoryDW = new Factory();

      _factoryWIC = new ImagingFactory2(); // initialize the WIC factory

      SetupRenderPipelines();
      SetupRenderStrategies();
      SetupFonts();
    }

    private void SetupFonts()
    {
      FontManager.ResourceFontLoader.LoadFonts(_factoryDW);
    }

    /// <summary>
    /// Setups all <see cref="IRenderStrategy"/>s.
    /// </summary>
    private void SetupRenderStrategies()
    {
      _renderStrategies = new List<IRenderStrategy>
        {
          new Default(_setup), 
          new VSync(_setup), 
          new MaxPerformance(_setup)
        };
      _currentRenderStrategyIndex = 0;
      _renderStrategies.ForEach(r => r.SetTargetFrameRate(_swapChain.Description.ModeDescription.RefreshRate));
    }

    /// <summary>
    /// Setups all <see cref="IRenderPipeline"/>s.
    /// </summary>
    private void SetupRenderPipelines()
    {
      _renderPipelines = new List<IRenderPipeline>
        {
          new SinglePass2DRenderPipeline(),
          new SBSRenderPipeline(),
          new TABRenderPipeline(),
          new SBS2DRenderPipeline(),
          new TAB2DRenderPipeline(),
        };
      _currentRenderPipeplineIndex = 0;
    }

    /// <summary>
    /// Fires an event if listeners are available.
    /// </summary>
    /// <param name="eventHandler"></param>
    private void Fire(EventHandler eventHandler)
    {
      try
      {
        if (eventHandler != null)
          eventHandler(null, EventArgs.Empty);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error executing render event handler:", e);
      }
    }

    private static void TryDispose<TE>(ref TE disposable)
    {
      IDisposable disp = disposable as IDisposable;
      if (disp != null)
        disp.Dispose();
      disposable = default(TE);
    }

    #endregion
  }
}
