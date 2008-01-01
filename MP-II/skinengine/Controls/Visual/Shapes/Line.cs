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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using MediaPortal.Core.InputManager;
using SkinEngine;
using SkinEngine.DirectX;

using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = Microsoft.DirectX.Matrix;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls.Visuals
{
  public class Line : Shape
  {

    Property _x1Property;
    Property _y1Property;
    Property _x2Property;
    Property _y2Property;

    public Line()
    {
      Init();
    }

    public Line(Line s)
      : base(s)
    {
      Init();
      X1 = s.X1;
      Y1 = s.Y1;
      X2 = s.X2;
      Y2 = s.Y2;
    }

    public override object Clone()
    {
      return new Line(this);
    }

    void Init()
    {
      _x1Property = new Property(0.0);
      _y1Property = new Property(0.0);
      _x2Property = new Property(0.0);
      _y2Property = new Property(0.0);
    }


    /// <summary>
    /// Gets or sets the x1 property.
    /// </summary>
    /// <value>The x1 property.</value>
    public Property X1Property
    {
      get
      {
        return _x1Property;
      }
      set
      {
        _x1Property = value;
      }
    }

    /// <summary>
    /// Gets or sets the x1.
    /// </summary>
    /// <value>The x1.</value>
    public double X1
    {
      get
      {
        return (double)_x1Property.GetValue();
      }
      set
      {
        _x1Property.SetValue(value);
      }
    }

    public Property Y1Property
    {
      get
      {
        return _y1Property;
      }
      set
      {
        _y1Property = value;
      }
    }

    /// <summary>
    /// Gets or sets the y1.
    /// </summary>
    /// <value>The y1.</value>
    public double Y1
    {
      get
      {
        return (double)_y1Property.GetValue();
      }
      set
      {
        _y1Property.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the x2 property.
    /// </summary>
    /// <value>The x2 property.</value>
    public Property X2Property
    {
      get
      {
        return _x2Property;
      }
      set
      {
        _x2Property = value;
      }
    }

    /// <summary>
    /// Gets or sets the x2.
    /// </summary>
    /// <value>The x2.</value>
    public double X2
    {
      get
      {
        return (double)_x2Property.GetValue();
      }
      set
      {
        _x2Property.SetValue(value);
      }
    }

    public Property Y2Property
    {
      get
      {
        return _y2Property;
      }
      set
      {
        _y2Property = value;
      }
    }

    /// <summary>
    /// Gets or sets the y2.
    /// </summary>
    /// <value>The y2.</value>
    public double Y2
    {
      get
      {
        return (double)_y2Property.GetValue();
      }
      set
      {
        _y2Property.SetValue(value);
      }
    }



    /// <summary>
    /// Performs the layout.
    /// </summary>
    protected override void PerformLayout()
    {
      Free();
      float cx;
      float cy;
      double w = Width; if (w == 0) w = ActualWidth;
      double h = Height; if (h == 0) h = ActualHeight;
      Vector3 orgPos = new Vector3(ActualPosition.X, ActualPosition.Y, ActualPosition.Z);
      //Fill brush
      GraphicsPath path;
      PointF[] vertices;
      PositionColored2Textured[] verts;

      //border brush

      ActualPosition = new Vector3(orgPos.X, orgPos.Y, orgPos.Z);
      if (Stroke != null && StrokeThickness > 0)
      {
        path = GetLine(new RectangleF(ActualPosition.X, ActualPosition.Y, (float)w, (float)h), (float)(StrokeThickness), out cx, out cy);
        vertices = ConvertPathToTriangleStrip(path, (int)cx, (int)(cy), (float)(StrokeThickness));

        _vertexBufferBorder = new VertexBuffer(typeof(PositionColored2Textured), vertices.Length, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
        verts = (PositionColored2Textured[])_vertexBufferBorder.Lock(0, 0);
        unchecked
        {
          for (int i = 0; i < vertices.Length; ++i)
          {
            verts[i].X = vertices[i].X;
            verts[i].Y = vertices[i].Y;
            verts[i].Z = 1.0f;
          }
        }
        Stroke.SetupBrush(this, ref verts);
        _vertexBufferBorder.Unlock();
        _verticesCountBorder = (verts.Length /3);
      }

    }

    #region Get the desired Rounded Rectangle path.
    private GraphicsPath GetLine(RectangleF baseRect, float thickNess, out float cx, out float cy)
    {
      float x1 = (float)(X1 + baseRect.X);
      float y1 = (float)(Y1 + baseRect.Y);
      float x2 = (float)(X2 + baseRect.X);
      float y2 = (float)(Y2 + baseRect.Y);

      float w = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
      cx = (float)(x1 + (w) / 2);
      cy = (float)(y1 + (thickNess) / 2);
      float ang = (float)((y2 - y1) / (x2 - x1));
      ang = (float)Math.Atan(ang);
      ang *= (float)(180.0f / Math.PI);
      System.Drawing.Drawing2D.Matrix matrix = new System.Drawing.Drawing2D.Matrix();
      matrix.RotateAt(ang, new PointF(cx, cy));
      GraphicsPath mPath = new GraphicsPath();
      System.Drawing.Rectangle r = new System.Drawing.Rectangle((int)x1, (int)y1, (int)w, (int)thickNess);
      mPath.AddRectangle(r);
      mPath.CloseFigure();
      mPath.Transform(matrix);
      mPath.Flatten();
      return mPath;
    }
    #endregion

  }
}
