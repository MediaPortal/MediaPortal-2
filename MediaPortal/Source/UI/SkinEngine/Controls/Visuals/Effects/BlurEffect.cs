using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public sealed class BlurEffect : ImageEffect
  {
    #region Consts

    private const string EFFECT_BLUR = "effects\\zoom_blur";

    #endregion

    #region Protected fields

    protected AbstractProperty _radiusProperty;

    #endregion


    #region Ctor & maintainance

    public BlurEffect()
    {
      _partialShaderEffect = EFFECT_BLUR;
      Init();
    }

    void Init()
    {
      _radiusProperty = new SProperty(typeof(double), 1.0);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BlurEffect el = (BlurEffect) source;
      Radius = el.Radius;
    }

    #endregion

    #region Properties

    public AbstractProperty RadiusProperty
    {
      get { return _radiusProperty; }
    }

    public double Radius
    {
      get { return (double) _radiusProperty.GetValue(); }
      set { _radiusProperty.SetValue(value); }
    }

    #endregion
  }
}
