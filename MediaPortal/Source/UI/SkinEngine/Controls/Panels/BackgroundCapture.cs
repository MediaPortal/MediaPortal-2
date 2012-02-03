using System.Drawing;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SlimDX;
using SlimDX.Direct3D9;
using Effect = MediaPortal.UI.SkinEngine.Controls.Visuals.Effects.Effect;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  /// <summary>
  /// BackgroundCapture is used as helper control to capture the already rendered screen from the backbuffer, so that any <see cref="Effect"/> can be applied to it.
  /// This component always captures the complete backbuffer size. The position inside the XAML code does matter, because only elements that were rendered before 
  /// are visible. It only processes the backbuffer if a <see cref="Effect"/> is set and rendering is required.
  /// </summary>
  public class BackgroundCapture : FrameworkElement
  {
    private Texture _texture;

    public override void Render(RenderContext parentRenderContext)
    {
      if (!IsVisible || Effect == null)
        return;

      RectangleF bounds = ActualBounds;
      if (bounds.Width <= 0 || bounds.Height <= 0)
        return;

      Matrix? layoutTransformMatrix = LayoutTransform == null ? new Matrix?() : LayoutTransform.GetTransform();
      Matrix? renderTransformMatrix = RenderTransform == null ? new Matrix?() : RenderTransform.GetTransform();

      RenderContext localRenderContext = parentRenderContext.Derive(bounds, layoutTransformMatrix, renderTransformMatrix, RenderTransformOrigin, Opacity);
      _inverseFinalTransform = Matrix.Invert(localRenderContext.MouseTransform);

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

      Effect effect = Effect;

      UpdateEffectMask(localRenderContext.OccupiedTransformedBounds, desc.Width, desc.Height, localRenderContext.ZOrder);
      if (effect.BeginRender(_texture, new RenderContext(Matrix.Identity, Matrix.Identity, 1.0d, bounds, localRenderContext.ZOrder)))
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

