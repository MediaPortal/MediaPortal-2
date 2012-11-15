#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Drawing;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SlimDX;
using SlimDX.Direct3D9;
using Effect = MediaPortal.UI.SkinEngine.Controls.Visuals.Effects.Effect;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Helper control to capture the already rendered screen from the backbuffer, so that any <see cref="Effect"/> can be applied to it.
  /// This component always captures the complete backbuffer size. The position inside the XAML code does matter, because only elements that
  /// were rendered before are visible. It only processes the backbuffer if a <see cref="Effect"/> is set and rendering is required.
  /// </summary>
  public class BackgroundEffect : FrameworkElement
  {
    private Texture _texture;

    public override void Render(RenderContext parentRenderContext)
    {
      Effect effect = Effect;
      if (!IsVisible || effect == null)
        return;

      RectangleF bounds = ActualBounds;
      if (bounds.Width <= 0 || bounds.Height <= 0)
        return;

      Matrix? layoutTransformMatrix = LayoutTransform == null ? new Matrix?() : LayoutTransform.GetTransform();
      Matrix? renderTransformMatrix = RenderTransform == null ? new Matrix?() : RenderTransform.GetTransform();

      RenderContext localRenderContext = parentRenderContext.Derive(bounds, layoutTransformMatrix, renderTransformMatrix, RenderTransformOrigin, Opacity);
      _inverseFinalTransform = Matrix.Invert(localRenderContext.Transform);

      DeviceEx device = SkinContext.Device;
      Surface backBuffer = device.GetRenderTarget(0);
      SurfaceDescription desc = backBuffer.Description;
      SurfaceDescription? textureDesc = _texture == null ? new SurfaceDescription?() : _texture.GetLevelDescription(0);
      if (!textureDesc.HasValue || textureDesc.Value.Width != desc.Width || textureDesc.Value.Height != desc.Height)
      {
        TryDispose(ref _texture);
        _texture = new Texture(device, desc.Width, desc.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
      }
      device.StretchRectangle(backBuffer, _texture.GetSurfaceLevel(0), TextureFilter.None);

      UpdateEffectMask(effect, localRenderContext.OccupiedTransformedBounds, desc.Width, desc.Height, localRenderContext.ZOrder);
      if (effect.BeginRender(_texture, new RenderContext(Matrix.Identity, 1.0d, bounds, localRenderContext.ZOrder)))
      {
        _effectContext.Render(0);
        effect.EndRender();
      }
    }

    public override void Dispose()
    {
      base.Dispose();
      TryDispose(ref _texture);
    }
  }
}

