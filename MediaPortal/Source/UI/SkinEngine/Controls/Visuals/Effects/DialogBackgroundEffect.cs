using System.Collections.Generic;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public class DialogBackgroundEffect : ShaderEffect
  {
    public DialogBackgroundEffect()
    {
      _shaderEffectName = "dialogbg";
    }

    protected override Dictionary<string, object> GetShaderParameters()
    {
      return new Dictionary<string, object>();
    }
  }
}
