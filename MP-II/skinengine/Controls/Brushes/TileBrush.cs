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
using MediaPortal.Core.Properties;
using SkinEngine.DirectX;
using SkinEngine.Controls.Visuals;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls.Brushes
{
  public enum AlignmentX { Left, Center, Right };
  public enum AlignmentY { Top, Center, Bottom };

  public enum Stretch
  {
    //The content preserves its original size.
    None,

    //The content is resized to fill the destination dimensions. The aspect ratio is not preserved.
    Fill,

    //The content is resized to fit in the destination dimensions while it preserves its native aspect ratio.
    Uniform,

    //The content is resized to fill the destination dimensions while it preserves its native aspect ratio. 
    //If the aspect ratio of the destination rectangle differs from the source, the source content is 
    //clipped to fit in the destination dimensions (zoom-in)
    UniformToFill
  };

  public class TileBrush : Brush
  {
    Property _alignmentXProperty;
    Property _alignmentYProperty;
    Property _stretchProperty;

    public TileBrush()
    {
      _alignmentXProperty = new Property(AlignmentX.Center);
      _alignmentYProperty = new Property(AlignmentY.Center);
      _stretchProperty = new Property(Stretch.Fill);
    }

    public Property AlignmentXProperty
    {
      get
      {
        return _alignmentXProperty;
      }
      set
      {
        _alignmentXProperty = value;
      }
    }

    public AlignmentX AlignmentX
    {
      get
      {
        return (AlignmentX)_alignmentXProperty.GetValue();
      }
      set
      {
        _alignmentXProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property AlignmentYProperty
    {
      get
      {
        return _alignmentYProperty;
      }
      set
      {
        _alignmentYProperty = value;
      }
    }

    public AlignmentY AlignmentY
    {
      get
      {
        return (AlignmentY)_alignmentYProperty.GetValue();
      }
      set
      {
        _alignmentYProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property StretchProperty
    {
      get
      {
        return _stretchProperty;
      }
      set
      {
        _stretchProperty = value;
      }
    }

    public Stretch Stretch
    {
      get
      {
        return (Stretch)_stretchProperty.GetValue();
      }
      set
      {
        _stretchProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public override void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      // todo here:
      ///   - stretchmode
      ///   - tilemode
      ///   - alignmentx/alignmenty
      ///   - viewbox
      
      for (int i = 0; i < verts.Length; ++i)
      {
        float u, v;
        float x1, y1;
        y1 = (float)verts[i].Y;
        v = (float)(y1 - element.ActualPosition.Y);
        v /= (float)(element.ActualHeight);

        x1 = (float)verts[i].X;
        u = (float)(x1 - element.ActualPosition.X);
        u /= (float)(element.ActualWidth);
        Scale(ref u, ref v);

        if (u < 0) u = 0;
        if (u > 1) u = 1;
        if (v < 0) v = 0;
        if (v > 1) v = 1;
        unchecked
        {
          ColorValue color = ColorValue.FromArgb((int)0xffffffff);
          color.Alpha *= (float)Opacity;
          verts[i].Color = color.ToArgb();
        }
        verts[i].Tu1 = u;
        verts[i].Tv1 = v;
        verts[i].Tu2 = 0;
        verts[i].Tv2 = 0;
      }
    }

    protected virtual void Scale(ref float u, ref float v)
    {
    }

    protected virtual Vector2 BrushDimensions
    {
      get
      {
        return new Vector2(1, 1);
      }
    }
  }
}
