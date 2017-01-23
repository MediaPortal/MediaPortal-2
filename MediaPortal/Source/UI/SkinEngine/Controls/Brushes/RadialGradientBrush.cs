#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using SharpDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class RadialGradientBrush : GradientBrush
  {
    #region Consts

    protected const string EFFECT_RADIALGRADIENT = "radialgradient";
    protected const string EFFECT_RADIALOPACITYGRADIENT = "radialgradient_opacity";

    protected const string PARAM_TRANSFORM = "g_transform";
    protected const string PARAM_RELATIVE_TRANSFORM = "g_relativetransform";
    protected const string PARAM_OPACITY = "g_opacity";
    protected const string PARAM_FOCUS = "g_focus";
    protected const string PARAM_CENTER = "g_center";
    protected const string PARAM_RADIUS = "g_radius";

    protected const string PARAM_ALPHATEX = "g_alphatex";
    protected const string PARAM_UPPERVERTSBOUNDS = "g_uppervertsbounds";
    protected const string PARAM_LOWERVERTSBOUNDS = "g_lowervertsbounds";

    #endregion

    #region Protected fields

    protected EffectAsset _effect;

    protected AbstractProperty _centerProperty;
    protected AbstractProperty _gradientOriginProperty;
    protected AbstractProperty _radiusXProperty;
    protected AbstractProperty _radiusYProperty;
    protected GradientBrushTexture _gradientBrushTexture;
    protected float[] g_focus;
    protected float[] g_center;
    protected float[] g_radius;
    protected Matrix g_relativetransform;
    protected volatile bool _refresh = false;

    #endregion

    #region Ctor

    public RadialGradientBrush()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    void Init()
    {
      _centerProperty = new SProperty(typeof(Vector2), new Vector2(0.5f, 0.5f));
      _gradientOriginProperty = new SProperty(typeof(Vector2), new Vector2(0.5f, 0.5f));
      _radiusXProperty = new SProperty(typeof(double), 0.5);
      _radiusYProperty = new SProperty(typeof(double), 0.5);
    }

    void Attach()
    {
      _centerProperty.Attach(OnPropertyChanged);
      _gradientOriginProperty.Attach(OnPropertyChanged);
      _radiusXProperty.Attach(OnPropertyChanged);
      _radiusYProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _centerProperty.Detach(OnPropertyChanged);
      _gradientOriginProperty.Detach(OnPropertyChanged);
      _radiusXProperty.Detach(OnPropertyChanged);
      _radiusYProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      RadialGradientBrush b = (RadialGradientBrush) source;
      Center = copyManager.GetCopy(b.Center);
      GradientOrigin = copyManager.GetCopy(b.GradientOrigin);
      RadiusX = b.RadiusX;
      RadiusY = b.RadiusY;
      _refresh = true;
      Attach();
    }

    #endregion

    protected override void OnRelativeTransformChanged(IObservable trans)
    {
      _refresh = true;
      base.OnRelativeTransformChanged(trans);
    }

    #region Protected methods

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      _refresh = true;
      base.OnPropertyChanged(prop, oldValue);
    }

    #endregion

    #region Public properties

    public AbstractProperty CenterProperty
    {
      get { return _centerProperty; }
    }

    public Vector2 Center
    {
      get { return (Vector2) _centerProperty.GetValue(); }
      set { _centerProperty.SetValue(value); }
    }

    public AbstractProperty GradientOriginProperty
    {
      get { return _gradientOriginProperty; }
    }

    public Vector2 GradientOrigin
    {
      get { return (Vector2) _gradientOriginProperty.GetValue(); }
      set { _gradientOriginProperty.SetValue(value); }
    }

    public AbstractProperty RadiusXProperty
    {
      get { return _radiusXProperty; }
    }

    public double RadiusX
    {
      get { return (double) _radiusXProperty.GetValue(); }
      set { _radiusXProperty.SetValue(value); }
    }

    public AbstractProperty RadiusYProperty
    {
      get { return _radiusYProperty; }
    }

    public double RadiusY
    {
      get { return (double) _radiusYProperty.GetValue(); }
      set { _radiusYProperty.SetValue(value); }
    }

    public override Texture Texture
    {
      get { return _gradientBrushTexture.Texture; }
    }

    #endregion

    #region Public methods

    public override void SetupBrush(FrameworkElement parent, ref PositionColoredTextured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);
      _refresh = true;
    }

    protected override bool BeginRenderBrushOverride(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    {
      if (_gradientBrushTexture == null || _refresh)
      {
        _gradientBrushTexture = BrushCache.Instance.GetGradientBrush(GradientStops);
        if (_gradientBrushTexture == null)
          return false;
      }

      Matrix finalTransform = renderContext.Transform.Clone();
      if (_refresh)
      {
        _refresh = false;
        _gradientBrushTexture = BrushCache.Instance.GetGradientBrush(GradientStops);
        _effect = ContentManager.Instance.GetEffect("radialgradient");

        g_focus = new float[] { GradientOrigin.X, GradientOrigin.Y };
        g_center = new float[] { Center.X, Center.Y };
        g_radius = new float[] { (float) RadiusX, (float) RadiusY };

        if (MappingMode == BrushMappingMode.Absolute)
        {
          g_focus[0] /= _vertsBounds.Width;
          g_focus[1] /= _vertsBounds.Height;

          g_center[0] /= _vertsBounds.Width;
          g_center[1] /= _vertsBounds.Height;

          g_radius[0] /= _vertsBounds.Width;
          g_radius[1] /= _vertsBounds.Height;
        }
        g_relativetransform = RelativeTransform == null ? Matrix.Identity : Matrix.Invert(RelativeTransform.GetTransform());
      }

      _effect.Parameters[PARAM_RELATIVE_TRANSFORM] = g_relativetransform;
      _effect.Parameters[PARAM_TRANSFORM] = GetCachedFinalBrushTransform();
      _effect.Parameters[PARAM_FOCUS] = g_focus;
      _effect.Parameters[PARAM_CENTER] = g_center;
      _effect.Parameters[PARAM_RADIUS] = g_radius;
      _effect.Parameters[PARAM_OPACITY] = (float) (Opacity * renderContext.Opacity);

      GraphicsDevice.Device.SetSamplerState(0, SamplerState.AddressU, SpreadAddressMode);
      _effect.StartRender(_gradientBrushTexture.Texture, finalTransform);

      return true;
    }

    public override void EndRender()
    {
      if (_effect != null)
        _effect.EndRender();
    }

    #endregion
  }
}
