using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public class PixelateEffect : ImageEffect
  {
    #region Consts

    private const string EFFECT_PIXELATE = "effects\\pixelate";

    #endregion

    #region Protected fields

    protected AbstractProperty _horizonzalPixelCountProperty;
    protected AbstractProperty _verticalPixelCountProperty;

    protected Dictionary<string, object> _effectParameters = new Dictionary<string, object>();

    #endregion

    #region Ctor & maintainance

    public PixelateEffect()
    {
      _partialShaderEffect = EFFECT_PIXELATE;
      Init();
    }

    void Init()
    {
      _horizonzalPixelCountProperty = new SProperty(typeof(int), 80);
      _verticalPixelCountProperty = new SProperty(typeof(int), 80);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      PixelateEffect el = (PixelateEffect) source;
      HorizonzalPixelCount = el.HorizonzalPixelCount;
      VerticalPixelCount = el.VerticalPixelCount;
    }

    #endregion

    #region Properties

    public AbstractProperty HorizonzalPixelCountProperty
    {
      get { return _horizonzalPixelCountProperty; }
    }

    public int HorizonzalPixelCount
    {
      get { return (int) _horizonzalPixelCountProperty.GetValue(); }
      set { _horizonzalPixelCountProperty.SetValue(value); }
    }

    public AbstractProperty VerticalPixelCountProperty
    {
      get { return _verticalPixelCountProperty; }
    }

    public int VerticalPixelCount
    {
      get { return (int) _verticalPixelCountProperty.GetValue(); }
      set { _verticalPixelCountProperty.SetValue(value); }
    }

    #endregion

    protected override Dictionary<string, object> GetShaderParameters()
    {
      _effectParameters["g_horizontalPixelCounts"] = (float) HorizonzalPixelCount;
      _effectParameters["g_verticalPixelCounts"] = (float) VerticalPixelCount;
      return _effectParameters;
    }
  }
}
