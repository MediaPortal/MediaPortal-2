using System.Collections.Generic;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public class ShaderBlurEffect : ShaderEffect
  {
    public ShaderBlurEffect()
    {
      _shaderEffectName = "blur";
    }

    protected override Dictionary<string, object> GetShaderParameters()
    {
      return new Dictionary<string, object>();
    }
  }
}
