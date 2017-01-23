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
using MediaPortal.UI.SkinEngine.DirectX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  /// <summary>
  /// <see cref="TemporaryRenderTarget"/> allows rendering to texture surface. It automatically sets
  /// the RenderTarget of the GraphicsDevice and restores all changes at the end (in Dispose).
  /// </summary>
  public class TemporaryRenderTarget : IDisposable
  {
    // The current D3D device
    private readonly DeviceEx _device = GraphicsDevice.Device;

    // Support multiple render target indexes
    private readonly int _renderTargetIndex = 0;

    // The target surface to render on
    private readonly Surface _targetSurface;

    // The active RendertTarget before we change it
    private readonly Surface _backBuffer;

    // Only need to dispose surface if we created them
    private readonly bool _disposeSurface = false;

    /// <summary>
    /// Constructs a <see cref="TemporaryRenderTarget"/> instance.
    /// </summary>
    /// <param name="targetSurface">Target surface to render on</param>
    public TemporaryRenderTarget(Surface targetSurface) :
      this(0, targetSurface)
    { }

    /// <summary>
    /// Constructs a <see cref="TemporaryRenderTarget"/> instance.
    /// </summary>
    /// <param name="targetTexture">Target texture to render on</param>
    public TemporaryRenderTarget(Texture targetTexture) :
      this(0, targetTexture)
    { }

    /// <summary>
    /// Constructs a <see cref="TemporaryRenderTarget"/> instance.
    /// </summary>
    /// <param name="renderTargetIndex">Render target index</param>
    /// <param name="targetTexture">Target texture to render on</param>
    public TemporaryRenderTarget(int renderTargetIndex, Texture targetTexture)
      : this(renderTargetIndex, targetTexture.GetSurfaceLevel(0))
    {
      _disposeSurface = true;
    }


    /// <summary>
    /// Constructs a <see cref="TemporaryRenderTarget"/> instance.
    /// </summary>
    /// <param name="renderTargetIndex">Render target index</param>
    /// <param name="targetSurface">Target surface to render on</param>
    public TemporaryRenderTarget(int renderTargetIndex, Surface targetSurface)
    {
      // Select target index (0 by default)
      _renderTargetIndex = renderTargetIndex;

      // Remember old RenderTarget
      _backBuffer = _device.GetRenderTarget(_renderTargetIndex);

      // Get information of new Texture target
      _targetSurface = targetSurface;

      // Set new target
      _device.SetRenderTarget(_renderTargetIndex, _targetSurface);
    }

    public void Dispose()
    {
      // Restore all previous rembered values
      using(_backBuffer)
        _device.SetRenderTarget(_renderTargetIndex, _backBuffer);

      if (_disposeSurface && _targetSurface != null)
        _targetSurface.Dispose();
    }
  }
}
