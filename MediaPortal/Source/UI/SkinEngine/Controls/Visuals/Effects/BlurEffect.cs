using System.Drawing;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public sealed class BlurEffect : Effect
  {
    #region Consts

    private const string EFFECT_BLUR = "effects\\zoom_blur";

    #endregion

    #region Protected fields

    protected AbstractProperty _radiusProperty;
    protected bool _refresh = true;
    protected readonly RectangleF FULLSIZE = new RectangleF(0, 0, 1, 1);

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
      _imageContext.ShaderEffect = EFFECT_BLUR;

      Vector4 lastFrameData = new Vector4(rect.Width, rect.Height, 0.0f, 0.0f);
      _imageContext.StartRender(renderContext, frameSize, texture, FULLSIZE, 0, lastFrameData);
      return true;
    }

    public override void EndRender()
    {
      _imageContext.EndRender();
    }

    #endregion
  }
}
