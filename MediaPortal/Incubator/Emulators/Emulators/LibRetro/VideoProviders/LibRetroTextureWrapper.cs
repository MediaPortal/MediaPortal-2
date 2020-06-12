using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.VideoProviders
{
  public class LibRetroTextureWrapper : ITextureProvider
  {
    #region SynchronizedTexture
    protected class SynchronizedTexture : Texture
    {
      protected readonly object _syncRoot = new object();
      protected bool _isDisposing;

      public SynchronizedTexture(Device device, int width, int height, int levelCount, Usage usage, Format format, Pool pool)
        : base(device, width, height, levelCount, usage, format, pool)
      {
        Disposing += OnDisposing;
      }

      public object SyncRoot
      {
        get { return _syncRoot; }
      }

      public bool IsDisposing
      {
        get { return _isDisposing; }
      }

      protected void OnDisposing(object sender, EventArgs e)
      {
        lock (_syncRoot)
          _isDisposing = true;
      }
    }
    #endregion

    const int INT_COUNT_PER_PIXEL = 1;
    const int BYTE_COUNT_PER_PIXEL = 4;
    const int TEXTURE_BUFFER_LENGTH = 2;

    protected SynchronizedTexture[] _textures = new SynchronizedTexture[TEXTURE_BUFFER_LENGTH];
    protected int _currentTextureIndex;
    protected Device _device = SkinContext.Device;

    public Texture Texture
    {
      get { return _textures[_currentTextureIndex]; }
    }

    public void UpdateTexture(int[] pixels, int width, int height, bool bottomLeftOrigin)
    {
      UpdateTexture(pixels, width, height, INT_COUNT_PER_PIXEL, bottomLeftOrigin);
    }

    public void UpdateTexture(byte[] pixels, int width, int height, bool bottomLeftOrigin)
    {
      UpdateTexture(pixels, width, height, BYTE_COUNT_PER_PIXEL, bottomLeftOrigin);
    }

    protected void UpdateTexture<T>(T[] pixels, int width, int height, int countPerPixel, bool bottomLeftOrigin) where T : struct
    {
      if (pixels == null || width * height * countPerPixel >= pixels.Length)
        return;

      SynchronizedTexture texture = null;
      try
      {
        texture = GetOrCreateTexture(width, height, Usage.Dynamic);
        lock (texture.SyncRoot)
        {
          if (texture.IsDisposing)
            return;
          DataStream dataStream;
          DataRectangle rectangle = texture.LockRectangle(0, LockFlags.Discard, out dataStream);
          int padding = rectangle.Pitch - (width * sizeof(int));
          int countPerLine = width * countPerPixel;

          using (dataStream)
          {
            if (bottomLeftOrigin)
              WritePixelsBottomLeft(pixels, height, countPerLine, padding, dataStream);
            else
              WritePixels(pixels, height, countPerLine, padding, dataStream);
            texture.UnlockRectangle(0);
          }
        }
      }
      catch (Exception ex)
      {
        if (texture != null)
        {
          texture.Dispose();
          _textures[_currentTextureIndex] = null;
        }
        ServiceRegistration.Get<ILogger>().Error("LibRetroTextureWrapper: Texture update failed", ex);
      }
    }

    public void UpdateTexture(Texture source, int width, int height, bool bottomLeftOrigin)
    {
      SynchronizedTexture texture = null;
      try
      {
        texture = GetOrCreateTexture(width, height, Usage.RenderTarget);
        lock (texture.SyncRoot)
        {
          if (texture.IsDisposing)
            return;
          _device.StretchRectangle(source.GetSurfaceLevel(0), new Rectangle(0, 0, width, height), texture.GetSurfaceLevel(0), null, TextureFilter.None);
        }
      }
      catch (Exception ex)
      {
        if (texture != null)
        {
          texture.Dispose();
          _textures[_currentTextureIndex] = null;
        }
        ServiceRegistration.Get<ILogger>().Error("LibRetroTextureWrapper: Texture update failed", ex);
      }
    }

    protected SynchronizedTexture GetOrCreateTexture(int width, int height, Usage usage)
    {
      _currentTextureIndex = (_currentTextureIndex + 1) % _textures.Length;
      SynchronizedTexture texture = _textures[_currentTextureIndex];

      if (texture != null)
      {
        lock (texture.SyncRoot)
        {
          if (texture.IsDisposing)
            texture = null;
          else
          {
            SurfaceDescription surface = texture.GetLevelDescription(0);
            if (surface.Usage != usage || surface.Width != width || surface.Height != height)
            {
              texture.Dispose();
              texture = null;
            }
          }
        }
      }

      if (texture == null)
      {
        texture = new SynchronizedTexture(_device, width, height, 1, usage, Format.X8R8G8B8, Pool.Default);
        _textures[_currentTextureIndex] = texture;
      }
      return texture;
    }

    protected void WritePixels<T>(T[] pixels, int height, int countPerLine, int padding, DataStream dataStream) where T : struct
    {
      for (int i = 0; i < height; i++)
      {
        if (padding > 0 && i > 0)
          dataStream.Position += padding;
        dataStream.WriteRange(pixels, i * countPerLine, countPerLine);
      }
    }

    protected void WritePixelsBottomLeft<T>(T[] pixels, int height, int countPerLine, int padding, DataStream dataStream) where T : struct
    {
      for (int i = height - 1; i >= 0; i--)
      {
        if (padding > 0 && i < height - 1)
          dataStream.Position += padding;
        dataStream.WriteRange(pixels, i * countPerLine, countPerLine);
      }
    }

    public void Release()
    {
      for (int i = 0; i < _textures.Length; i++)
      {
        if (_textures[i] != null)
        {
          _textures[i].Dispose();
          _textures[i] = null;
        }
      }
      ServiceRegistration.Get<ILogger>().Debug("LibRetroTextureWrapper: Released");
    }

    public void Dispose()
    {
      Release();
    }
  }
}