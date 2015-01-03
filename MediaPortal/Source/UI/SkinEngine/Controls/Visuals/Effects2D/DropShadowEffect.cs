#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX;
using SharpDX.Direct2D1.Effects;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects2D
{
  /// <summary>
  /// Provides a base class for all bitmap effects.
  /// </summary>
  public class DropShadowEffect : Effect
  {
    protected Shadow _shadow;
    protected AffineTransform2D _transform;
    protected Composite _composite;

    protected AbstractProperty _shadowDepthProperty;
    protected AbstractProperty _directionProperty;
    protected AbstractProperty _blurRadiusProperty;
    protected AbstractProperty _colorProperty;
    protected AbstractProperty _opacityProperty;

    public DropShadowEffect()
    {
      _shadowDepthProperty = new SProperty(typeof(float), 0f);
      _directionProperty = new SProperty(typeof(float), 0f);
      _blurRadiusProperty = new SProperty(typeof(float), 5f);
      _colorProperty = new SProperty(typeof(Color), Color.Black);
      _opacityProperty = new SProperty(typeof(float), 1f);
      Attach();
    }

    void Attach()
    {
      _shadowDepthProperty.Attach(EffectChanged);
      _directionProperty.Attach(EffectChanged);
      _blurRadiusProperty.Attach(EffectChanged);
      _colorProperty.Attach(EffectChanged);
      _opacityProperty.Attach(EffectChanged);
    }

    void Detach()
    {
      _shadowDepthProperty.Detach(EffectChanged);
      _directionProperty.Detach(EffectChanged);
      _blurRadiusProperty.Detach(EffectChanged);
      _colorProperty.Detach(EffectChanged);
      _opacityProperty.Detach(EffectChanged);
    }

    public float ShadowDepth
    {
      get { return (float)_shadowDepthProperty.GetValue(); }
      set { _shadowDepthProperty.SetValue(value); }
    }

    public AbstractProperty ShadowDepthProperty
    {
      get { return _shadowDepthProperty; }
    }

    public float Direction
    {
      get { return (float)_directionProperty.GetValue(); }
      set { _directionProperty.SetValue(value); }
    }

    public AbstractProperty DirectionProperty
    {
      get { return _directionProperty; }
    }

    public float BlurRadius
    {
      get { return (float)_blurRadiusProperty.GetValue(); }
      set { _blurRadiusProperty.SetValue(value); }
    }

    public AbstractProperty BlurRadiusProperty
    {
      get { return _blurRadiusProperty; }
    }

    public float Opacity
    {
      get { return (float)_opacityProperty.GetValue(); }
      set { _opacityProperty.SetValue(value); }
    }

    public AbstractProperty OpacityProperty
    {
      get { return _opacityProperty; }
    }

    public Color Color
    {
      get { return (Color)_colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    public AbstractProperty ColorProperty
    {
      get { return _colorProperty; }
    }

    public override SharpDX.Direct2D1.Effect Output
    {
      get
      {
        return _composite;
      }
    }

    private void EffectChanged(AbstractProperty property, object oldvalue)
    {
      UpdateEffectParams();
    }

    public override void Allocate()
    {
      if (_input == null)
      {
        Deallocate();
        return;
      }

      _shadow = new Shadow(GraphicsDevice11.Instance.Context2D1);
      _shadow.SetInput(0, _input, true);

      _transform = new AffineTransform2D(GraphicsDevice11.Instance.Context2D1);
      _transform.SetInputEffect(0, _shadow);

      _composite = new Composite(GraphicsDevice11.Instance.Context2D1);
      _composite.SetInputEffect(0, _transform);
      _composite.SetInput(1, _input, true);

      UpdateEffectParams();
    }


    private void UpdateEffectParams()
    {
      if (_shadow != null)
      {
        _shadow.BlurStandardDeviation = BlurRadius;
        // If there is an additional Opacity, premultiply Alpha of Color
        _shadow.Color = Opacity != 1f ? new Color4(Color.ToColor3(), Color.A * Opacity) : Color;
        _shadow.Cached = true;
      }
      if (_transform != null)
      {
        _transform.TransformMatrix = Matrix3x2.Identity;
        if (ShadowDepth != 0f)
        {
          // Transform depth+angle into delta x / y
          var dx = ShadowDepth * Math.Sin(Direction);
          var dy = ShadowDepth * Math.Cos(Direction);
          _transform.TransformMatrix *= Matrix3x2.Translation((float)dx, (float)dy);
        }

        _transform.Cached = true;
      }

    }

    public override void Deallocate()
    {
      Detach();
      TryDispose(ref _composite);
      TryDispose(ref _transform);
      TryDispose(ref _shadow);
    }
  }
}
