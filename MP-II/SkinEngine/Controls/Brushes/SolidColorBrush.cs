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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.Effects;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Rendering;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Brushes
{
  public class SolidColorBrush : Brush//, IAsset
  {
    #region Private properties

    Property _colorProperty;
    //Texture _texture;
    double _height;
    double _width;
    EffectAsset _effect;
    EffectHandleAsset _effectHandleColor;
    DateTime _lastTimeUsed;

    #endregion

    #region Ctor

    public SolidColorBrush()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _colorProperty = new Property(typeof(Color), Color.White);
      //ContentManager.Add(this);
      _effect = ContentManager.GetEffect("solidbrush");
      _effectHandleColor = _effect.GetParameterHandle("g_solidColor");
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
      Color = copyManager.GetCopy(b.Color);
      Attach();
    }

    #endregion

    /// <summary>
    /// Called when a property changed.
    /// </summary>
    /// <param name="prop">The prop.</param>
    protected override void OnPropertyChanged(Property prop)
    {
      Fire();
    }

    public Property ColorProperty
    {
      get { return _colorProperty; }
    }

    public Color Color
    {
      get { return (Color)_colorProperty.GetValue(); }
      set { _colorProperty.SetValue(value); }
    }

    public override void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      //Trace.WriteLine("SolidColorBrush.SetupBrush()");
      //if (_texture == null || element.ActualHeight != _height || element.ActualWidth != _width)
      {
        UpdateBounds(element, ref verts);
        base.SetupBrush(element, ref verts);
        Color4 color = ColorConverter.FromColor(this.Color);
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

    public override bool BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {
      //if (_texture == null) return;

      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      Color4 v = ColorConverter.FromColor(this.Color);
      v.Alpha *= (float)SkinContext.Opacity;
      _effectHandleColor.SetParameter(v);
      _effect.StartRender(null);
      //GraphicsDevice.Device.SetTexture(0, null);
      _lastTimeUsed = SkinContext.Now;
      return true;
    }

    public override  void SetupPrimitive(PrimitiveContext context)
    {
      Color4 v = ColorConverter.FromColor(this.Color);
      v.Alpha *= (float)SkinContext.Opacity;
      context.Effect = _effect;
      context.Parameters = new EffectParameters();
      context.Parameters.Add(_effectHandleColor, v);
    }

    public override void EndRender()
    {
      if (_effect != null)
      {
        _effect.EndRender();
      }
    }

  }
}
