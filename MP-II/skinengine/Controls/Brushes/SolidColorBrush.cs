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
using SkinEngine.DirectX;
using System.Drawing;
using Microsoft.DirectX.Direct3D;
using SkinEngine;
namespace SkinEngine.Controls.Brushes
{
  public class SolidColorBrush : Brush, IAsset
  {
    Property _colorProperty;
    Texture _texture;
    double _height;
    double _width;
    EffectAsset _effect;
    DateTime _lastTimeUsed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolidColorBrush"/> class.
    /// </summary>
    public SolidColorBrush()
    {
      _colorProperty = new Property(Color.White);
      ContentManager.Add(this);
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
    public override void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      if (_texture == null || element.ActualHeight != _height || element.ActualWidth != _width)
      {
        base.SetupBrush(element, ref verts);
        if (_texture != null)
        {
          _texture.Dispose();
        }
        _height = element.ActualHeight;
        _width = element.ActualWidth;
        _texture = new Texture(GraphicsDevice.Device, 2, 2, 0, Usage.None, Format.A8R8G8B8, Pool.Managed);
      }
    }

    public override void BeginRender()
    {
      if (_texture == null) return;
      ColorValue color = ColorValue.FromArgb(Color.ToArgb());
      color.Alpha *= (float)Opacity;
      _effect = ContentManager.GetEffect("solidbrush");
      _effect.Parameters["g_solidColor"] = color;
      _effect.StartRender(_texture);
      _lastTimeUsed = SkinContext.Now;
    }

    public override void EndRender()
    {
      if (_effect != null)
      {
        _effect.EndRender();
        _effect = null;
      }
    }

    #region IAsset Members

    public bool IsAllocated
    {
      get
      {
        return (_texture != null);
      }
    }

    public bool CanBeDeleted
    {
      get
      {
        if (!IsAllocated)
        {
          return false;
        }
        TimeSpan ts = SkinContext.Now - _lastTimeUsed;
        if (ts.TotalSeconds >= 1)
        {
          return true;
        }

        return false;
      }
    }

    public void Free()
    {
      if (_texture != null)
      {
        _texture.Dispose();
        _texture = null;
      }
    }

    #endregion
  }
}
