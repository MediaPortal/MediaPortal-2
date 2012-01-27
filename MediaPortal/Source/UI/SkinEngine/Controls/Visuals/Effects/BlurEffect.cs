using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;
using SlimDX;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public sealed class BlurEffect : Effect
  {
    private const string EFFECT_BLUR = "blur";

    #region Protected fields

    protected AbstractProperty _radiusProperty;
    protected EffectAsset _effect;

    #endregion


    #region Ctor & maintainance

    public BlurEffect()
    {
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

    #region Rendering

    public void Render(RenderContext renderContext)
    {
      _effect = ContentManager.Instance.GetEffect(EFFECT_BLUR);
      SetEffectParameters(renderContext);
      // TODO: what to render? The parent element?
      // _effect.StartRender(Matrix.Identity);

    }

    protected void SetEffectParameters(RenderContext renderContext)
    {
      // TODO: pass proper parameters to shader (here radius)
      //_effect.Parameters[PARAM_RELATIVE_TRANSFORM] = _relativeTransformCache;
    }

    #endregion
  }
}
