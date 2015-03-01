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
using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  /// <summary>
  /// <see cref="TemporaryRenderTarget2D"/> allows rendering to a custom render target. It automatically sets
  /// the RenderTarget and the Transform of the GraphicsDevice and restores all changes at the end (in Dispose).
  /// </summary>
  public class TemporaryRenderTarget2D : IDisposable
  {
    // The current D3D device
    private readonly DeviceContext _context = GraphicsDevice11.Instance.Context2D1;

    private readonly Image _backBuffer;
    private readonly Matrix3x2 _transform;

    /// <summary>
    /// Constructs a <see cref="TemporaryRenderTarget"/> instance.
    /// </summary>
    /// <param name="targetSurface">Target surface to render on</param>
    public TemporaryRenderTarget2D(Bitmap1 targetSurface)
    {
      // Make sure to flush all drawing calls to current target before restoring old values
      _context.Flush();

      // Remember old RenderTarget
      _backBuffer = _context.Target;
      _transform = _context.Transform;

      // Set new target
      _context.Target = targetSurface;
    }

    public void Dispose()
    {
      // Make sure to flush all drawing calls to current target before restoring old values
      _context.Flush();
      _context.Target = _backBuffer;
      _context.Transform = _transform;
    }
  }
}
