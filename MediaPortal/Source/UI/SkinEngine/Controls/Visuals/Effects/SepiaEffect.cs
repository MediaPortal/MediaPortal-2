namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public class SepiaEffect : ImageEffect
  {
    private const string EFFECT_SEPIA = "effects\\sepia";

    public SepiaEffect()
    {
      _partialShaderEffect = EFFECT_SEPIA;
    }
  }
}
