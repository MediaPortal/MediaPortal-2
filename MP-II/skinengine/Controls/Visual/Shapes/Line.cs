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
using Matrix = SlimDX.Matrix;

using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;

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
      _x1Property.Attach(new PropertyChangedHandler(OnCoordinateChanged));
      _y1Property.Attach(new PropertyChangedHandler(OnCoordinateChanged));
      _x2Property.Attach(new PropertyChangedHandler(OnCoordinateChanged));
      _y2Property.Attach(new PropertyChangedHandler(OnCoordinateChanged));
    }

    void OnCoordinateChanged(Property property)
    {
      Invalidate();
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
      Trace.WriteLine("Line.PerformLayout() " + this.Name);
      Free();
      double w = ActualWidth;
      double h = ActualHeight;
      float centerX, centerY;
      SizeF rectSize = new SizeF((float)w, (float)h);
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        m.InvertSize(ref rectSize);
      }
      System.Drawing.RectangleF rect = new System.Drawing.RectangleF((float)ActualPosition.X, (float)ActualPosition.Y, rectSize.Width, rectSize.Height);

      //Fill brush
      GraphicsPath path;
      PositionColored2Textured[] verts;

      //border brush

      if (Stroke != null && StrokeThickness > 0)
      {
        using (path = GetLine(rect))
        {
          CalcCentroid(path, out centerX, out centerY);
          _vertexBufferBorder = ConvertPathToTriangleFan(path, centerX, centerY, out verts);
          if (_vertexBufferBorder != null)
          {
            Stroke.SetupBrush(this, ref verts);
            PositionColored2Textured.Set(_vertexBufferBorder, ref verts);
            _verticesCountBorder = (verts.Length / 3);
          }
        }
      }

    }
    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      if (!IsVisible) return;
      if ((Stroke != null && _vertexBufferBorder == null&& StrokeThickness>0) || _performLayout)
      {
        PerformLayout();
        _performLayout = false;
      }
      if (Stroke != null && StrokeThickness > 0)
      {
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Stroke.BeginRender(_vertexBufferBorder, _verticesCountBorder, PrimitiveType.TriangleFan))
        {
          GraphicsDevice.Device.SetStreamSource(0, _vertexBufferBorder, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, _verticesCountBorder);
          Stroke.EndRender();
        }
      }

      base.DoRender();
      _lastTimeUsed = SkinContext.Now;
    }

    #region Get the desired Rounded Rectangle path.
    private GraphicsPath GetLine(RectangleF baseRect)
    {
      float x1 = (float)(X1 + baseRect.X);
      float y1 = (float)(Y1 + baseRect.Y);
      float x2 = (float)(X2 + baseRect.X);
      float y2 = (float)(Y2 + baseRect.Y);

      float w = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

      float ang = (float)((y2 - y1) / (x2 - x1));
      ang = (float)Math.Atan(ang);
      ang *= (float)(180.0f / Math.PI);
      GraphicsPath mPath = new GraphicsPath();
      System.Drawing.Rectangle r = new System.Drawing.Rectangle((int)x1, (int)y1, (int)w, (int)StrokeThickness);
      mPath.AddRectangle(r);
      mPath.CloseFigure();

      System.Drawing.Drawing2D.Matrix matrix = new System.Drawing.Drawing2D.Matrix();
      matrix.RotateAt(ang, new PointF(x1, y1), MatrixOrder.Append);

      matrix.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
      matrix.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        matrix.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
      }
      matrix.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      mPath.Transform(matrix);
      mPath.Flatten();

      return mPath;
    }
    #endregion

  }
}
