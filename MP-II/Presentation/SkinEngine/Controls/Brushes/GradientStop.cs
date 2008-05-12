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
using System.Drawing;
using MediaPortal.Presentation.Properties;

namespace Presentation.SkinEngine.Controls.Brushes
{
  public class GradientStop : Property, ICloneable
  {
    Property _colorProperty;
    Property _offsetProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="GradientStop"/> class.
    /// </summary>
    public GradientStop(): base(null)
    {
      Init();
    }

    public GradientStop(GradientStop b): base(null)
    {
      Init();
      Color = b.Color;
      Offset = b.Offset;
    }
    void Init()
    {
      _colorProperty = new Property(typeof(Color), Color.White);
      _offsetProperty = new Property(typeof(double), 0.0);
      _colorProperty.Attach(OnColorChanged);
      _offsetProperty.Attach(OnOffsetChanged);
    }

    public virtual object Clone()
    {
      return new GradientStop(this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GradientStop"/> class.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <param name="color">The color.</param>
    public GradientStop(double offset, Color color): base(null)
    {
      _colorProperty = new Property(typeof(Color), color);
      _offsetProperty = new Property(typeof(double), offset);
    }

    public void OnColorChanged(Property prop)
    {
      Fire();
    }

    public void OnOffsetChanged(Property prop)
    {
      Fire();
    }

    /// <summary>
    /// Gets or sets the color property.
    /// </summary>
    /// <value>The color property.</value>
    public Property ColorProperty
    {
      get
      {
        return _colorProperty;
      }
      set
      {
        _colorProperty = value;
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
        return (Color) _colorProperty.GetValue();
      }
      set
      {
        _colorProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the offset property.
    /// </summary>
    /// <value>The offset property.</value>
    public Property OffsetProperty
    {
      get
      {
        return _offsetProperty;
      }
      set
      {
        _offsetProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the offset.
    /// </summary>
    /// <value>The offset.</value>
    public double Offset
    {
      get
      {
        return (double) _offsetProperty.GetValue();
      }
      set
      {
        _offsetProperty.SetValue(value);
      }
    }
  }
}
