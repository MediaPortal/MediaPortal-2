#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using Presentation.SkinEngine;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Effects;
using Presentation.SkinEngine.DirectX;
using Presentation.SkinEngine.Rendering;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Brushes
{
  public class SolidColorBrush : Brush//, IAsset
  {
    Property _colorProperty;
    //Texture _texture;
    double _height;
    double _width;
    EffectAsset _effect;
    EffectHandleAsset _effectHandleColor;
    DateTime _lastTimeUsed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
    /// </summary>
    public SolidColorBrush()
    {
      Init();
    }

    public SolidColorBrush(SolidColorBrush b)
      : base(b)
    {
      Init();
      Color = b.Color;
    }

    void Init()
    {
      _colorProperty = new Property(typeof(Color), Color.White);
      //ContentManager.Add(this);
      _colorProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _effect = ContentManager.GetEffect("solidbrush");
      _effectHandleColor = _effect.GetParameterHandle("g_solidColor");
    }

    public override object Clone()
    {
      SolidColorBrush result = new SolidColorBrush(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    /// <summary>
    /// Gets the color property.
    /// </summary>
    /// <value>The color property.</value>
    public Property ColorProperty
    {
      get
      {
        return _colorProperty;
      }
    }


    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    /// <value>The color.</value>
    public Color Color
    {
      get
      {
        return (Color)_colorProperty.GetValue();
      }
      set
      {
        _colorProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Called when a property changed.
    /// </summary>
    /// <param name="prop">The prop.</param>
    protected override void OnPropertyChanged(Property prop)
    {
      Fire();
    }

    /// <summary>
    /// Setups the brush.
    /// </summary>
    /// <param name="element">The element.</param>
    public override void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      //Trace.WriteLine("SolidColorBrush.SetupBrush()");
      //if (_texture == null || element.ActualHeight != _height || element.ActualWidth != _width)
      {
        UpdateBounds(element, ref verts);
        base.SetupBrush(element, ref verts);
        ColorValue color = ColorConverter.FromColor(this.Color);
        color.Alpha *= (float)Opacity;
        for (int i = 0; i < verts.Length; ++i)
        {
          verts[i].Color = color.ToArgb();
        }
        _height = element.ActualHeight;
        _width = element.ActualWidth;
        //if (_texture == null)
        //{
        //  _texture = new Texture(GraphicsDevice.Device, 2, 2, 0, Usage.None, Format.A8R8G8B8, Pool.Managed);
        //}
      }
    }

    /// <summary>
    /// Begins the render.
    /// </summary>
    public override bool BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {
      //if (_texture == null) return;

      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      ColorValue v = ColorConverter.FromColor(this.Color);
      v.Alpha *= (float)SkinContext.Opacity;
      _effectHandleColor.SetParameter(v);
      _effect.StartRender(null);
      //GraphicsDevice.Device.SetTexture(0, null);
      _lastTimeUsed = SkinContext.Now;
      return true;
    }

    public override  void SetupPrimitive(PrimitiveContext context)
    {
      ColorValue v = ColorConverter.FromColor(this.Color);
      v.Alpha *= (float)SkinContext.Opacity;
      context.Effect = _effect;
      context.Parameters = new EffectParameters();
      context.Parameters.Add(_effectHandleColor, v);
    }

    /// <summary>
    /// Ends the render.
    /// </summary>
    public override void EndRender()
    {
      if (_effect != null)
      {
        _effect.EndRender();
      }
    }

  }
}
