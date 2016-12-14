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
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class SolidColorBrush : Brush
  {
    #region Consts

    protected const string EFFECT_SOLID = "solid";
    protected const string EFFECT_SOLIDOPACITY = "solid_opacity";

    protected const string PARAM_SOLIDCOLOR = "g_solidcolor";

    #endregion

    #region Protected properties

    protected AbstractProperty _colorProperty;
    protected EffectAsset _effect;

    #endregion

    #region Ctor

    public SolidColorBrush()
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
      _colorProperty = new SProperty(typeof(Color), Color.White);
    }

    void Attach()
    {
      _colorProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _colorProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      SolidColorBrush b = (SolidColorBrush) source;
      Color = b.Color;
      Attach();
    }

    #endregion

    public AbstractProperty ColorProperty
    {
      get { return _colorProperty; }
    }

    public Color Color
    {
      get { return (Color) _colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    protected override bool BeginRenderBrushOverride(PrimitiveBuffer primitiveBuffer, RenderContext renderContext)
    {
      Matrix finalTransform = renderContext.Transform.Clone();
      Color4 v = ColorConverter.FromColor(Color);
      v.Alpha *= (float) (Opacity * renderContext.Opacity);
      _effect = ContentManager.Instance.GetEffect(EFFECT_SOLID);
      _effect.Parameters[PARAM_SOLIDCOLOR] = v;
      _effect.StartRender(finalTransform);
      return true;
    }

    public override void EndRender()
    {
      if (_effect != null)
        _effect.EndRender();
    }
  }
}
