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
  public class Polygon : Shape
  {
    Property _pointsProperty;

    public Polygon()
    {
      Init();
    }

    public Polygon(Polygon s)
      : base(s)
    {
      Init();
      foreach (Point point in s.Points)
      {
        Points.Add(point);
      }
    }

    public override object Clone()
    {
      return new Polygon(this);
    }

    void Init()
    {
      _pointsProperty = new Property(new PointCollection());
    }


    public Property PointsProperty
    {
      get
      {
        return _pointsProperty;
      }
      set
      {
        _pointsProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the points.
    /// </summary>
    /// <value>The points.</value>
    public PointCollection Points
    {
      get
      {
        return (PointCollection)_pointsProperty.GetValue();
      }
      set
      {
        _pointsProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Performs the layout.
    /// </summary>
    protected override void PerformLayout()
    {
      Trace.WriteLine("Polygon.PerformLayout()");
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
      PositionColored2Textured[] verts;
      GraphicsPath path;
      PointF[] vertices;
      if (Fill != null)
      {
        using (path = GetPolygon(rect))
        {
          CalcCentroid(path, out centerX, out centerY);
          vertices = ConvertPathToTriangleFan(path, centerX, centerY);
          _vertexBufferFill = new VertexBuffer(typeof(PositionColored2Textured), vertices.Length, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
          verts = (PositionColored2Textured[])_vertexBufferFill.Lock(0, 0);
          unchecked
          {
            for (int i = 0; i < vertices.Length; ++i)
            {
              verts[i].X = vertices[i].X;
              verts[i].Y = vertices[i].Y;
              verts[i].Z = 1.0f;
            }
          }
          Fill.SetupBrush(this, ref verts);
          _vertexBufferFill.Unlock();
          _verticesCountFill = (verts.Length - 2);
        }
      }
      //border brush

      if (Stroke != null && StrokeThickness > 0)
      {
        using (path = GetPolygon(rect))
        {
          CalcCentroid(path, out centerX, out centerY);
          vertices = ConvertPathToTriangleStrip(path, centerX, centerY, (float)StrokeThickness);

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
          _verticesCountBorder = (verts.Length / 3);
        }
      }

    }
    #region Get the desired Rounded Rectangle path.
    private GraphicsPath GetPolygon(RectangleF baseRect)
    {
      Point[] points = new Point[Points.Count];
      for (int i = 0; i < Points.Count; ++i)
      {
        points[i] = (Point)Points[i];
      }
      GraphicsPath mPath = new GraphicsPath();
      mPath.AddPolygon(points);
      mPath.CloseFigure();

      System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
      m.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
      m.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      mPath.Transform(m);

      mPath.Flatten();


      return mPath;
    }
    #endregion


  }
}
