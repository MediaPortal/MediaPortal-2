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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX.Direct2D1;
using SharpDX.Direct2D1.Effects;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects2D
{
  /// <summary>
  /// Blurs the control using a given radius.
  /// </summary>
  public class BlurEffect : Effect
  {
    protected GaussianBlur _blur;

    protected AbstractProperty _radiusProperty;
    protected AbstractProperty _renderingBiasProperty;
    protected AbstractProperty _borderModeProperty;

    public BlurEffect()
    {
      _radiusProperty = new SProperty(typeof(float), 3f);
      _renderingBiasProperty = new SProperty(typeof(GaussianBlurOptimization), GaussianBlurOptimization.Balanced);
      _borderModeProperty = new SProperty(typeof(BorderMode), BorderMode.Soft);
      Attach();
    }

    void Attach()
    {
      _radiusProperty.Attach(EffectChanged);
      _borderModeProperty.Attach(EffectChanged);
      _renderingBiasProperty.Attach(EffectChanged);
    }

    void Detach()
    {
      _radiusProperty.Detach(EffectChanged);
      _borderModeProperty.Detach(EffectChanged);
      _renderingBiasProperty.Detach(EffectChanged);
    }

    public float Radius
    {
      get { return (float)_radiusProperty.GetValue(); }
      set { _radiusProperty.SetValue(value); }
    }

    public AbstractProperty RadiusProperty
    {
      get { return _radiusProperty; }
    }

    public float Opacity
    {
      get { return (float)_renderingBiasProperty.GetValue(); }
      set { _renderingBiasProperty.SetValue(value); }
    }

    public AbstractProperty RenderingBiasProperty
    {
      get { return _renderingBiasProperty; }
    }

    public BorderMode BorderMode
    {
      get { return (BorderMode)_borderModeProperty.GetValue(); }
      set { _borderModeProperty.SetValue(value); }
    }

    public AbstractProperty BorderModeProperty
    {
      get { return _borderModeProperty; }
    }

    public override SharpDX.Direct2D1.Effect Output
    {
      get
      {
        return _blur;
      }
    }

    protected override void EffectChanged(AbstractProperty property, object oldvalue)
    {
      UpdateEffectParams();
    }

    public override bool Allocate()
    {
      if (!base.Allocate())
        return false;

      _blur = new GaussianBlur(GraphicsDevice11.Instance.Context2D1);
      _blur.SetInput(0, _input, true);

      UpdateEffectParams();
      return true;
    }

    private void UpdateEffectParams()
    {
      if (_blur != null)
      {
        _blur.StandardDeviation = Radius;
        _blur.Optimization = GaussianBlurOptimization.Speed;
        _blur.Cached = Cache;
      }
    }

    public override void Deallocate()
    {
      base.Deallocate();
      Detach();
      TryDispose(ref _blur);
    }
  }
}
