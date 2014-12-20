using System;
using System.Windows.Forms;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;

namespace MediaPortal.UI.SkinEngine.DirectX11
{
  internal class GraphicsDevice11 : IDisposable
  {
    private SharpDX.Direct3D11.Device _device3D;
    private SharpDX.Direct3D11.Device1 _device3D1;
    private SharpDX.DXGI.Device _deviceDXGI;
    private SharpDX.Direct2D1.Device _device2D1;

    private SwapChain _swapChain;
    private Texture2D _backBufferTexture;
    private Surface1 _backBuffer;
    private SharpDX.Direct2D1.DeviceContext _context2D;
    private Bitmap1 _renderTarget2D;

    private static GraphicsDevice11 _instance;

    public static GraphicsDevice11 Instance
    {
      get { return _instance ?? (_instance = new GraphicsDevice11()); }
    }

    public Form RenderTarget { get; internal set; }

    public Surface1 BackBuffer
    {
      get { return _backBuffer; }
    }

    public Texture2D BackBufferTexture
    {
      get { return _backBufferTexture; }
    }

    public SharpDX.Direct3D11.Device Device3D
    {
      get { return _device3D; }
    }

    public SharpDX.Direct3D11.Device1 Device3D1
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

    public SharpDX.Direct2D1.DeviceContext Context2D1
    {
      get { return _context2D; }
    }

    public Bitmap1 RenderTarget2D
    {
      get { return _renderTarget2D; }
    }

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
    }

    public void CreateDevice()
    {
      // SwapChain description
      int width = RenderTarget.ClientSize.Width;
      int height = RenderTarget.ClientSize.Height;
      var desc = new SwapChainDescription
      {
        BufferCount = 1,
        ModeDescription = new ModeDescription(width, height, new Rational(50, 1), Format.R8G8B8A8_UNorm),
        IsWindowed = true,
        OutputHandle = RenderTarget.Handle,
        SampleDescription = new SampleDescription(1, 0),
        SwapEffect = SwapEffect.Discard,
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

      SharpDX.Direct3D11.Device.CreateWithSwapChain(DriverType.Hardware, flags, featureLevels, desc, out _device3D, out _swapChain);

      // New RenderTargetView from the backbuffer
      _backBufferTexture = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
      _backBuffer = _backBufferTexture.QueryInterface<Surface1>();

      _device3D1 = _device3D.QueryInterface<SharpDX.Direct3D11.Device1>(); // get a reference to the Direct3D 11.1 device
      _deviceDXGI = _device3D1.QueryInterface<SharpDX.DXGI.Device>(); // get a reference to DXGI device

      _device2D1 = new SharpDX.Direct2D1.Device(_deviceDXGI); // initialize the D2D device

      _context2D = new SharpDX.Direct2D1.DeviceContext(_device2D1, DeviceContextOptions.EnableMultithreadedOptimizations);

      _renderTarget2D = new Bitmap1(_context2D, _backBuffer);
    }

    private static void TryDispose<TE>(ref TE disposable)
    {
      IDisposable disp = disposable as IDisposable;
      if (disp != null)
        disp.Dispose();
      disposable = default(TE);
    }

    public void Dispose()
    {
      TryDispose(ref _backBuffer);
      TryDispose(ref _backBufferTexture);
      TryDispose(ref _swapChain);

      TryDispose(ref _renderTarget2D);
      TryDispose(ref _context2D);

      TryDispose(ref _device3D1);
      TryDispose(ref _device3D);
      TryDispose(ref _device2D1);
      TryDispose(ref _deviceDXGI);
    }
  }
}
