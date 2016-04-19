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
    /// Array containing different texture planes.
    /// </summary>
    private readonly Texture[] _planes = new Texture[2];
    private readonly DeviceEx _device = SkinContext.Device;
    private Sprite _sprite;

    /// <summary>
    /// Lock for syncronising the texture update and rendering
    /// </summary>
    private readonly object _syncObj = new Object();

    private readonly Action _onTextureInvalidated;

    public BluRayOSDRenderer(Action onTextureInvalidated)
    {
      _onTextureInvalidated = onTextureInvalidated;
      _sprite = new Sprite(_device);
    }

    /// <summary>
    /// Returns true if there is an OSD texture available.
    /// </summary>
    public bool IsOSDPresent
    {
      get
      {
        lock (_syncObj)
        {
          return _planes[0] != null || _planes[1] != null;
        }
      }
    }

    private void FreeResources()
    {
      lock (_syncObj)
      {
        FilterGraphTools.TryDispose(ref _planes[0]);
        FilterGraphTools.TryDispose(ref _planes[1]);
        FilterGraphTools.TryDispose(ref _sprite);
      }
    }

    public void DrawItem(BluRayAPI.OSDTexture item)
    {
      try
      {
        lock (_syncObj)
        {
          var index = (int)item.Plane;
          var isNewTexture = false;

          // Dispose old texture only if new texture for plane is different
          if (_planes[index] != null)
          {
            isNewTexture = _planes[index].NativePointer != item.Texture;
            if (isNewTexture)
              FilterGraphTools.TryDispose(ref _planes[index]);
          }

          if (item.Width == 0 || item.Height == 0 || item.Texture == IntPtr.Zero)
            return;

          if (_planes[index] == null || isNewTexture)
            _planes[index] = new Texture(item.Texture);
        }
      }
      catch (Exception ex)
      {
        BluRayPlayerBuilder.LogError(ex.ToString());
      }

      if (_onTextureInvalidated != null)
        _onTextureInvalidated();
    }

    public void DrawOverlay(Texture targetTexture)
    {
      if (targetTexture == null || !IsOSDPresent)
        return;

      try
      {
        lock (_syncObj)
        {
          // TemporaryRenderTarget changes RenderTarget to texture and restores settings when done (Dispose)
          using (new TemporaryRenderTarget(targetTexture))
          {
            foreach (var texture in _planes)
            {
              if (texture != null)
              {
                _sprite.Begin();
                _sprite.Draw(texture, new ColorBGRA(255, 255, 255, 255) /* White */);
                _sprite.End();
              }
            }
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
