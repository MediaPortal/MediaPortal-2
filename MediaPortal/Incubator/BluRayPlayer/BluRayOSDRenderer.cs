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

using System;
using System.Drawing;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using SharpDX.Direct3D9;
using Rectangle = SharpDX.Rectangle;

namespace MediaPortal.UI.Players.Video
{
  public class BluRayOSDRenderer : IDisposable
  {
    /// <summary>
    /// Texture containing the whole OSD area (1920x1080)
    /// </summary>
    private Texture _combinedOsdTexture;
    private Surface _combinedOsdSurface;
    private Sprite _sprite;
    private Size _fullOsdSize = new Size(1920, 1080);
    private const Format FORMAT = Format.A8R8G8B8;
    private readonly ColorBGRA _transparentColor = new ColorBGRA(0, 0, 0, 0);
    private readonly DeviceEx _device = SkinContext.Device;

    /// <summary>
    /// Lock for syncronising the texture update and rendering
    /// </summary>
    private readonly object _syncObj = new Object();

    private readonly Action _onTextureInvalidated;

    public BluRayOSDRenderer(Action onTextureInvalidated)
    {
      _onTextureInvalidated = onTextureInvalidated;
    }

    /// <summary>
    /// Returns true if there is an OSD texture available.
    /// </summary>
    public bool IsOSDPresent
    {
      get { return _combinedOsdTexture != null; }
    }

    private void InitTexture(BluRayAPI.OSDTexture item)
    {
      if (item.Width == 0 || item.Height == 0 || item.Texture == IntPtr.Zero)
      {
        FreeResources();
        return;
      }

      if (_combinedOsdTexture == null || _combinedOsdTexture.IsDisposed)
      {
        _combinedOsdTexture = new Texture(_device, _fullOsdSize.Width, _fullOsdSize.Height, 1, Usage.RenderTarget, FORMAT, Pool.Default);
        _combinedOsdSurface = _combinedOsdTexture.GetSurfaceLevel(0);

        _sprite = new Sprite(_device);

        Rectangle dstRect = new Rectangle(0, 0, _fullOsdSize.Width, _fullOsdSize.Height);
        _device.ColorFill(_combinedOsdSurface, dstRect, _transparentColor);
      }
    }

    private void FreeResources()
    {
      // _combinedOsdSurface does not need an explicit Dispose, it's done on Texture dispose:
      // When SlimDx.Configuration.DetectDoubleDispose is set to true, an ObjectDisposedException will be thrown!
      _combinedOsdSurface = null;
      FilterGraphTools.TryDispose(ref _combinedOsdTexture);
      FilterGraphTools.TryDispose(ref _sprite);
    }

    public void DrawItem(BluRayAPI.OSDTexture item)
    {
      try
      {
        lock (_syncObj)
        {
          InitTexture(item);
          if (_combinedOsdSurface != null)
          {
            Rectangle sourceRect = new Rectangle(0, 0, item.Width, item.Height);
            Rectangle dstRect = new Rectangle(item.X, item.Y, item.Width, item.Height);

            using (Texture itemTexture = new Texture(item.Texture))
              _device.StretchRectangle(itemTexture.GetSurfaceLevel(0), sourceRect, _combinedOsdSurface, dstRect, TextureFilter.None);
          }
        }
      }
      catch (Exception ex)
      {
        BluRayPlayerBuilder.LogError(ex.ToString());
      }

      if (_onTextureInvalidated != null)
        _onTextureInvalidated();
    }

    public void DrawOverlay(Surface targetSurface)
    {
      if (targetSurface == null || _combinedOsdSurface == null)
        return;

      try
      {
        lock (_syncObj)
        {
          // TemporaryRenderTarget changes RenderTarget to texture and restores settings when done (Dispose)
          using (new TemporaryRenderTarget(targetSurface))
          {
            _sprite.Begin();
            _sprite.Draw(_combinedOsdTexture, new ColorBGRA(255, 255, 255, 255) /* White */);
            _sprite.End();
          }
        }
      }
      catch (Exception ex)
      {
        BluRayPlayerBuilder.LogError(ex.ToString());
      }
    }

    #region IDisposable Member

    public void Dispose()
    {
      FreeResources();
    }

    #endregion
  }
}