namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public class InvertEffect : ImageEffect
  {
    private const string EFFECT_SEPIA = "effects\\invert";

    public InvertEffect()
    {
      _partialShaderEffect = EFFECT_SEPIA;
    }
  }
}
