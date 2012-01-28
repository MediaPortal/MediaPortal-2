using System.Drawing;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public abstract class ImageEffect : Effect
  {
    protected ImageContext _imageContext;
    protected string _partialShaderEffect;
    protected bool _refresh = true;

    #region Rendering

    protected override bool BeginRenderEffectOverride(Texture texture, RenderContext renderContext)
    {
      if (_refresh)
      {
        _imageContext = new ImageContext();
        _refresh = false;
      }

      RectangleF rect = renderContext.OccupiedTransformedBounds;
      SizeF frameSize = new SizeF(rect.Width, rect.Height);
      _imageContext.FrameSize = frameSize;
      _imageContext.ShaderEffect = _partialShaderEffect;

      Vector4 lastFrameData = new Vector4(rect.Width, rect.Height, 0.0f, 0.0f);
      _imageContext.StartRender(renderContext, frameSize, texture, CROP_FULLSIZE, 0, lastFrameData);
      return true;
    }

    public override void EndRender()
    {
      _imageContext.EndRender();
    }

    #endregion

  }
}
