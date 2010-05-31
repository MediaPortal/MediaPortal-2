#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using System.Drawing;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class SolidColorBrush : Brush
  {
    #region Private properties

    AbstractProperty _colorProperty;
    EffectAsset _effect;
    EffectHandleAsset _effectHandleColor;

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

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      FireChanged();
    }

    public AbstractProperty ColorProperty
    {
      get { return _colorProperty; }
    }

    public Color Color
    {
      get { return (Color) _colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    public override void SetupBrush(FrameworkElement parent, ref PositionColored2Textured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref verts, zOrder, adaptVertsToBrushTexture);
      _effect = ContentManager.GetEffect("solidbrush");
      _effectHandleColor = _effect.GetParameterHandle("g_solidColor");
    }

    public override bool BeginRenderBrush(PrimitiveContext primitiveContext, RenderContext renderContext)
    {
      Color4 v = ColorConverter.FromColor(Color);
      v.Alpha *= (float) renderContext.Opacity;
      _effectHandleColor.SetParameter(v);
      _effect.StartRender(renderContext.Transform);
      return true;
    }

    public override void EndRender()
    {
      if (_effect != null)
        _effect.EndRender();
    }
  }
}
