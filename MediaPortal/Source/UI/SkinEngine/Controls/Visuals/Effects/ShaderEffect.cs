using System.Collections.Generic;
using System.Drawing;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public abstract class ShaderEffect : Effect
  {
    protected EffectContext _effectContext;
    protected string _shaderEffectName;
    protected bool _refresh = true;

    #region Rendering

    protected override bool BeginRenderEffectOverride(Texture texture, RenderContext renderContext)
    {
      if (_refresh)
      {
        _effectContext = new EffectContext();
        _refresh = false;
      }

      RectangleF rect = renderContext.OccupiedTransformedBounds;
      SizeF frameSize = new SizeF(rect.Width, rect.Height);
      _effectContext.ExtraParameters = GetShaderParameters();
      _effectContext.ShaderEffect = _shaderEffectName;

      Vector4 lastFrameData = new Vector4(rect.Width, rect.Height, 0.0f, 0.0f);
      _effectContext.StartRender(renderContext, frameSize, texture, CROP_FULLSIZE, 0, lastFrameData);
      return true;
    }

    protected virtual Dictionary<string, object> GetShaderParameters()
    {
      return null;
    }

    public override void EndRender()
    {
      _effectContext.EndRender();
    }

    #endregion

  }
}
