using SharpGL.Version;
using SharpRetro.RetroGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpRetro.LibRetro;
using SharpDX.Direct3D9;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace Emulators.LibRetro.GLContexts
{
  public class RetroGLContext : IRetroGLContext
  {
    [DllImport("opengl32", EntryPoint = "wglGetProcAddress", ExactSpelling = true)]
    private static extern IntPtr wglGetProcAddress(IntPtr function_name);

    protected retro_hw_get_current_framebuffer_t _getCurrentFramebufferDlgt;
    protected retro_hw_get_proc_address_t _getProcAddressDlgt;
    protected retro_hw_context_reset_t _contextReset;
    protected retro_hw_context_reset_t _contextDestroy;
    protected DXRenderContextProvider _contextProvider;

    protected bool _hasDXContext;
    protected byte[] _pixels;
    protected bool _isCreated;
    protected bool _bottomLeftOrigin;
    protected bool _isTextureDirty;
    protected int _currentWidth;
    protected int _currentHeight;

    public bool BottomLeftOrigin
    {
      get { return _bottomLeftOrigin; }
    }

    public Texture Texture
    {
      get { return _hasDXContext && _contextProvider != null ? _contextProvider.CurrentTexture : null; }
    }

    public bool IsTextureDirty
    {
      get { return _isTextureDirty; }
      set { _isTextureDirty = value; }
    }

    public int CurrentWidth
    {
      get { return _currentWidth; }
    }

    public int CurrentHeight
    {
      get { return _currentHeight; }
    }

    public bool HasDXContext
    {
      get { return _hasDXContext; }
    }

    public retro_hw_get_current_framebuffer_t GetCurrentFramebufferDlgt
    {
      get { return _getCurrentFramebufferDlgt; }
    }

    public retro_hw_get_proc_address_t GetProcAddressDlgt
    {
      get { return _getProcAddressDlgt; }
    }

    public RetroGLContext()
    {
      _getCurrentFramebufferDlgt = new retro_hw_get_current_framebuffer_t(GetCurrentFramebuffer);
      _getProcAddressDlgt = new retro_hw_get_proc_address_t(GetProcAddress);
    }

    public void Init(bool depth, bool stencil, bool bottomLeftOrigin, retro_hw_context_reset_t contextReset, retro_hw_context_reset_t contextDestroy)
    {
      _bottomLeftOrigin = bottomLeftOrigin;
      _contextReset = contextReset;
      _contextDestroy = contextDestroy;
      ServiceRegistration.Get<ILogger>().Debug("RetroGLContextProvider: Initialised: depth: {0}, strencil: {1}, bottomLeftOrigin: {2}, contextDestroy: {3}", depth, stencil, bottomLeftOrigin, contextDestroy != null);
    }

    public void Create(int width, int height)
    {
      if (_isCreated)
        return;

      _contextProvider = new DXRenderContextProvider();
      _contextProvider.Create(OpenGLVersion.OpenGL2_1, new OpenGLEx(), width, height, 32, null);
      _hasDXContext = _contextProvider.HasDXContext;
      if (!_hasDXContext)
      {
        ServiceRegistration.Get<ILogger>().Warn("RetroGLContextProvider: WGL_NV_DX_interop extensions are not supported by the graphics driver, falling back to read back. This will reduce performance");
        _pixels = new byte[width * height * 4];
      }
      _isCreated = true;
      ServiceRegistration.Get<ILogger>().Debug("RetroGLContextProvider: Created OpengGL context: width: {0}, height: {1}", width, height);
      if (_contextReset != null)
        _contextReset();
    }

    public byte[] ReadPixels(int width, int height)
    {
      if (!_isCreated || _hasDXContext)
        return null;
      _contextProvider.ReadPixels(_pixels, width, height);
      return _pixels;
    }

    public void UpdateCurrentTexture(int width, int height)
    {
      if (!_isCreated || !_hasDXContext)
        return;
      _contextProvider.UpdateCurrentTexture(_bottomLeftOrigin);
      _currentWidth = width;
      _currentHeight = height;
      _isTextureDirty = true;
    }

    protected IntPtr GetProcAddress(IntPtr sym)
    {
      return wglGetProcAddress(sym);
    }

    protected uint GetCurrentFramebuffer()
    {
      return _contextProvider.FramebufferId;
    }

    public void Dispose()
    {
      if (_isCreated)
        _contextProvider.Destroy();
    }
  }
}