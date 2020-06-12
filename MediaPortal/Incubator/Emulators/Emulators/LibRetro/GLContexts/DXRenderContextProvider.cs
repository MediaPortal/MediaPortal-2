using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpGL;
using SharpDX.Direct3D9;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace Emulators.LibRetro.GLContexts
{
  public class DXRenderContextProvider : FBORenderContextProvider
  {
    protected static readonly string[] EXTENSION_FUNCTIONS = new[] { "wglDXSetResourceShareHandleNV", "wglDXOpenDeviceNV", "wglDXCloseDeviceNV", "wglDXRegisterObjectNV", "wglDXUnregisterObjectNV", "wglDXLockObjectsNV", "wglDXUnlockObjectsNV" };

    protected bool _hasDXContext;
    protected uint _outputFramebufferId;
    protected DeviceEx _device = SkinContext.Device;
    protected uint[] _textureIds;
    protected IntPtr _dxDeviceGLHandle;
    protected SharedTexture[] _dxTextures;
    protected Texture _currentTexture;

    public bool HasDXContext
    {
      get { return _hasDXContext; }
    }

    public Texture CurrentTexture
    {
      get { return _currentTexture; }
    }

    public void UpdateCurrentTexture(bool bottomLeftOrigin)
    {
      if (!_hasDXContext)
        return;
      Render(bottomLeftOrigin);
      if (_currentTexture == null)
        _currentTexture = _dxTextures[1].Texture;
    }

    protected void Render(bool bottomLeftOrigin)
    {
      gl.PushAttrib(OpenGL.GL_TEXTURE_BIT | OpenGL.GL_DEPTH_TEST | OpenGL.GL_LIGHTING);
      gl.Disable(OpenGL.GL_DEPTH_TEST);
      gl.Disable(OpenGL.GL_LIGHTING);
      gl.UseProgram(0);

      gl.MatrixMode(SharpGL.Enumerations.MatrixMode.Projection);
      gl.PushMatrix();
      gl.LoadIdentity();
      if (bottomLeftOrigin)
        gl.Scale(1, -1, 1);
      gl.Ortho(0, width, 0, height, -1, 1);

      gl.MatrixMode(SharpGL.Enumerations.MatrixMode.Modelview);
      gl.PushMatrix();
      gl.LoadIdentity();

      gl.DXLockObjectsNV(_dxDeviceGLHandle, new IntPtr[] { _dxTextures[1].GLHandle });
      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, _outputFramebufferId);

      gl.Enable(OpenGL.GL_TEXTURE_2D);
      gl.BindTexture(OpenGL.GL_TEXTURE_2D, _textureIds[0]);

      // Draw a textured quad
      gl.Begin(OpenGL.GL_QUADS);
      gl.TexCoord(0, 0); gl.Vertex(0, 0, 0);
      gl.TexCoord(0, 1); gl.Vertex(0, height, 0);
      gl.TexCoord(1, 1); gl.Vertex(width, height, 0);
      gl.TexCoord(1, 0); gl.Vertex(width, 0, 0);
      gl.End();

      gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
      gl.Disable(OpenGL.GL_TEXTURE_2D);

      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
      gl.DXUnlockObjectsNV(_dxDeviceGLHandle, new IntPtr[] { _dxTextures[1].GLHandle });

      gl.MatrixMode(SharpGL.Enumerations.MatrixMode.Projection);
      gl.PopMatrix();

      gl.MatrixMode(SharpGL.Enumerations.MatrixMode.Modelview);
      gl.PopMatrix();

      gl.PopAttrib();
    }

    protected override void CreateFramebuffers(int width, int height)
    {
      _hasDXContext = HasDXExtensions();
      if (!_hasDXContext)
      {
        base.CreateFramebuffers(width, height);
        return;
      }

      uint[] ids = new uint[2];
      //  First, create the frame buffer and bind it.
      gl.GenFramebuffersEXT(2, ids);
      _framebufferID = ids[0];
      _outputFramebufferId = ids[1];

      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, _framebufferID);
      CreateTextures(width, height);
      gl.DXLockObjectsNV(_dxDeviceGLHandle, new IntPtr[] { _dxTextures[0].GLHandle });
      gl.FramebufferTexture2DEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, OpenGL.GL_TEXTURE_2D, _textureIds[0], 0);
      CreateDepthBuffer(width, height);

      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, _outputFramebufferId);
      gl.DXLockObjectsNV(_dxDeviceGLHandle, new IntPtr[] { _dxTextures[1].GLHandle });
      gl.FramebufferTexture2DEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, OpenGL.GL_TEXTURE_2D, _textureIds[1], 0);
      gl.DXUnlockObjectsNV(_dxDeviceGLHandle, new IntPtr[] { _dxTextures[1].GLHandle });

      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
    }

    protected void CreateTextures(int width, int height)
    {
      _dxDeviceGLHandle = gl.DXOpenDeviceNV(_device.NativePointer);
      _dxTextures = new SharedTexture[2];
      _textureIds = new uint[2];
      gl.GenTextures(2, _textureIds);

      for (int i = 0; i < 2; i++)
      {
        IntPtr sharedResourceHandle = IntPtr.Zero;
        Texture texture = new Texture(_device, width, height, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default, ref sharedResourceHandle);
        bool result = gl.DXSetResourceShareHandleNV(texture.NativePointer, sharedResourceHandle);
        IntPtr glHandle = gl.DXRegisterObjectNV(_dxDeviceGLHandle, texture.NativePointer, _textureIds[i], OpenGL.GL_TEXTURE_2D, OpenGLEx.WGL_ACCESS_WRITE_DISCARD_NV);
        _dxTextures[i] = new SharedTexture { Texture = texture, GLHandle = glHandle };
      }
    }

    protected override void DestroyFramebuffers()
    {
      if (!_hasDXContext)
      {
        base.DestroyFramebuffers();
        return;
      }

      gl.DXUnlockObjectsNV(_dxDeviceGLHandle, new IntPtr[] { _dxTextures[0].GLHandle });
      for (int i = 0; i < 2; i++)
      {
        gl.DXUnregisterObjectNV(_dxDeviceGLHandle, _dxTextures[i].GLHandle);
        _dxTextures[i].Texture.Dispose();
      }
      gl.DeleteTextures(2, _textureIds);
      
      //  Delete the render buffers.
      gl.DeleteRenderbuffersEXT(1, new uint[] { _depthRenderBufferID });
      //	Delete the framebuffer.
      gl.DeleteFramebuffersEXT(2, new uint[] { _framebufferID, _outputFramebufferId });
      gl.DXCloseDeviceNV(_dxDeviceGLHandle);

      //  Reset the IDs.
      _currentTexture = null;
      _dxDeviceGLHandle = IntPtr.Zero;
      _dxTextures = new SharedTexture[0];
      _textureIds = new uint[0];
      _depthRenderBufferID = 0;
      _framebufferID = 0;
      _outputFramebufferId = 0;
    }

    protected bool HasDXExtensions()
    {
      return EXTENSION_FUNCTIONS.All(f => gl.IsExtensionFunctionSupported(f));
    }
  }
}