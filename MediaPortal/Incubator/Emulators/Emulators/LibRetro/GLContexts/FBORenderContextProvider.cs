using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpGL.Version;
using SharpGL.RenderContextProviders;
using SharpGL;

namespace Emulators.LibRetro.GLContexts
{
  public class FBORenderContextProvider : HiddenWindowRenderContextProvider
  {
    protected uint _colourRenderBufferID = 0;
    protected uint _depthRenderBufferID = 0;
    protected uint _framebufferID = 0;
    protected OpenGLEx gl;

    public uint FramebufferId
    {
      get { return _framebufferID; }
    }

    /// <summary>
    /// Creates the render context provider. Must also create the OpenGL extensions.
    /// </summary>
    /// <param name="openGLVersion">The desired OpenGL version.</param>
    /// <param name="gl">The OpenGL context.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="bitDepth">The bit depth.</param>
    /// <param name="parameter">The parameter</param>
    /// <returns></returns>
    public bool Create(OpenGLVersion openGLVersion, OpenGLEx gl, int width, int height, int bitDepth, object parameter)
    {
      this.gl = gl;
      //  Call the base class. 	        
      base.Create(openGLVersion, gl, width, height, bitDepth, parameter);
      CreateFramebuffers(width, height);
      return true;
    }

    public bool ReadPixels(byte[] buffer, int width, int height)
    {
      if (deviceContextHandle == IntPtr.Zero)
        return false;
      //  Set the read buffer.
      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, _framebufferID);
      gl.ReadBuffer(OpenGL.GL_COLOR_ATTACHMENT0_EXT);
      gl.ReadPixels(0, 0, width, height, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, buffer);
      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
      return true;
    }

    protected virtual void CreateFramebuffers(int width, int height)
    {
      uint[] ids = new uint[1];
      //  First, create the frame buffer and bind it.
      gl.GenFramebuffersEXT(1, ids);
      _framebufferID = ids[0];

      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, _framebufferID);
      CreateColourBuffer(width, height);
      CreateDepthBuffer(width, height);
      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
    }

    protected void CreateColourBuffer(int width, int height)
    {
      uint[] ids = new uint[1];
      //	Create the colour render buffer and bind it, then allocate storage for it.
      gl.GenRenderbuffersEXT(1, ids);
      _colourRenderBufferID = ids[0];
      gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, _colourRenderBufferID);
      gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_RGBA, width, height);
      gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT,
          OpenGL.GL_RENDERBUFFER_EXT, _colourRenderBufferID);
      gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, 0);
    }

    protected void CreateDepthBuffer(int width, int height)
    {
      uint[] ids = new uint[1];
      //	Create the depth render buffer and bind it, then allocate storage for it.
      gl.GenRenderbuffersEXT(1, ids);
      _depthRenderBufferID = ids[0];
      gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, _depthRenderBufferID);
      gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_DEPTH_COMPONENT24, width, height);
      gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_DEPTH_ATTACHMENT_EXT,
          OpenGL.GL_RENDERBUFFER_EXT, _depthRenderBufferID);
      gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, 0);
    }

    protected virtual void DestroyFramebuffers()
    {
      //  Delete the render buffers.
      gl.DeleteRenderbuffersEXT(2, new uint[] { _colourRenderBufferID, _depthRenderBufferID });
      //	Delete the framebuffer.
      gl.DeleteFramebuffersEXT(1, new uint[] { _framebufferID });
      //  Reset the IDs.
      _colourRenderBufferID = 0;
      _depthRenderBufferID = 0;
      _framebufferID = 0;
    }

    protected void CheckFramebufferStatus()
    {
      uint status = gl.CheckFramebufferStatusEXT(OpenGL.GL_FRAMEBUFFER_EXT);
      switch (status)
      {
        case OpenGL.GL_FRAMEBUFFER_COMPLETE_EXT:
          break;
        case OpenGL.GL_FRAMEBUFFER_UNSUPPORTED_EXT:
          throw new InvalidOperationException("Not supported framebuffer-format!");
        default:
          throw new InvalidOperationException(status.ToString());
      }
    }

    public override void SetDimensions(int width, int height)
    {
      //  Call the base.
      base.SetDimensions(width, height);
      DestroyFramebuffers();
      CreateFramebuffers(width, height);
    }

    public override void Destroy()
    {
      //  Delete the render buffers.
      DestroyFramebuffers();      
      //	Call the base, which will delete the render context handle and window.
      base.Destroy();
    }
  }
}