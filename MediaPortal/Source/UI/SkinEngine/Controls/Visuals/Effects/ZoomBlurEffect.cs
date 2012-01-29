using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public class ZoomBlurEffect : ImageEffect
  {
    #region Consts

    private const string EFFECT_BLUR = "effects\\zoom_blur";

    #endregion

    #region Protected fields

    protected AbstractProperty _centerXProperty;
    protected AbstractProperty _centerYProperty;
    protected AbstractProperty _blurAmountProperty;

    protected Dictionary<string, object> _effectParameters = new Dictionary<string, object>();

    #endregion


    #region Ctor & maintainance

    public ZoomBlurEffect()
    {
      _partialShaderEffect = EFFECT_BLUR;
      Init();
    }

    void Init()
    {
      _centerXProperty = new SProperty(typeof(double), 0.5);
      _centerYProperty = new SProperty(typeof(double), 0.5);
      _blurAmountProperty = new SProperty(typeof(double), 0.1);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ZoomBlurEffect el = (ZoomBlurEffect) source;
      CenterX = el.CenterX;
      CenterY = el.CenterY;
      BlurAmount = el.BlurAmount;
    }

    #endregion

    #region Properties

    public AbstractProperty CenterXProperty
    {
      get { return _centerXProperty; }
    }

    public double CenterX
    {
      get { return (double) _centerXProperty.GetValue(); }
      set { _centerXProperty.SetValue(value); }
    }

    public AbstractProperty CenterYProperty
    {
      get { return _centerYProperty; }
    }

    public double CenterY
    {
      get { return (double) _centerYProperty.GetValue(); }
      set { _centerYProperty.SetValue(value); }
    }

    public AbstractProperty BlurAmountProperty
    {
      get { return _blurAmountProperty; }
    }

    public double BlurAmount
    {
      get { return (double) _blurAmountProperty.GetValue(); }
      set { _blurAmountProperty.SetValue(value); }
    }

    #endregion

    protected override Dictionary<string, object> GetShaderParameters()
    {
      _effectParameters["g_centerX"] = (float) CenterX;
      _effectParameters["g_centerY"] = (float) CenterY;
      _effectParameters["g_blurAmount"] = (float) BlurAmount;
      return _effectParameters;
    }
  }
}
