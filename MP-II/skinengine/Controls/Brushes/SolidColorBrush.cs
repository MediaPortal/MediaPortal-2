#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using SkinEngine;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;
using SkinEngine.Effects;
using System.Drawing;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls.Brushes
{
  public class SolidColorBrush : Brush
  {
    Property _colorProperty;
    Texture _texture;
    double _height;
    double _width;
    EffectAsset _effect;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
    /// </summary>
    public SolidColorBrush()
    {
      _colorProperty = new Property(Color.White);
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
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Setups the brush.
    /// </summary>
    /// <param name="element">The element.</param>
    public override void SetupBrush(FrameworkElement element)
    {
      if (_texture == null || element.ActualHeight != _height || element.ActualWidth != _width)
      {
        if (_texture != null)
        {
          _texture.Dispose();
        }
        _height = element.ActualHeight;
        _width = element.ActualWidth;
        _texture = new Texture(GraphicsDevice.Device, (int)_width, (int)_height, 0, Usage.None, Format.A8R8G8B8, Pool.Managed);
      }
    }

    public override void BeginRender( )
    {
      if (_texture == null) return;
      ColorValue color = ColorValue.FromArgb(Color.ToArgb());
      _effect = ContentManager.GetEffect("solidbrush");
      _effect.Parameters["g_solidColor"] = color;
      _effect.StartRender(_texture);
    }
    public override void EndRender()
    {
      if (_effect != null)
      {
        _effect.EndRender();
        _effect = null;
      }
    }
  }
}
