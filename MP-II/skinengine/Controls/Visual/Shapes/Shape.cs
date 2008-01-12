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
using SkinEngine.Controls.Brushes;

using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = Microsoft.DirectX.Matrix;
using Brush = SkinEngine.Controls.Brushes.Brush;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using GeometryUtility;
namespace SkinEngine.Controls.Visuals
{
  public class Shape : FrameworkElement, IAsset
  {
    Property _stretchProperty;
    Property _fillProperty;
    Property _strokeProperty;
    Property _strokeThicknessProperty;
    protected VertexBuffer _vertexBufferFill;
    protected int _verticesCountFill;
    protected VertexBuffer _vertexBufferBorder;
    protected int _verticesCountBorder;
    protected DateTime _lastTimeUsed;
    protected bool _performLayout;

    public Shape()
    {
      Init();
    }

    public Shape(Shape s)
      : base(s)
    {
      Init();
      if (s.Fill != null)
        Fill = (Brush)s.Fill.Clone();
      if (s.Stroke != null)
        Stroke = (Brush)s.Stroke.Clone();
      StrokeThickness = s.StrokeThickness;
      Stretch = s.Stretch;
    }

    public override object Clone()
    {
      return new Shape(this);
    }

    void Init()
    {
      _fillProperty = new Property(null);
      _strokeProperty = new Property(null);
      _strokeThicknessProperty = new Property(1.0);
      _stretchProperty = new Property(Stretch.None);
      ContentManager.Add(this);
    }


    /// <summary>
    /// Gets or sets the stretch property.
    /// </summary>
    /// <value>The stretch property.</value>
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

    /// <summary>
    /// Gets or sets the stretch.
    /// </summary>
    /// <value>The stretch.</value>
    public Stretch Stretch
    {
      get
      {
        return (Stretch)_stretchProperty.GetValue();
      }
      set
      {
        _stretchProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the fill property.
    /// </summary>
    /// <value>The fill property.</value>
    public Property FillProperty
    {
      get
      {
        return _fillProperty;
      }
      set
      {
        _fillProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the fill.
    /// </summary>
    /// <value>The fill.</value>
    public Brush Fill
    {
      get
      {
        return _fillProperty.GetValue() as Brush;
      }
      set
      {
        _fillProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the stroke property.
    /// </summary>
    /// <value>The stroke property.</value>
    public Property StrokeProperty
    {
      get
      {
        return _strokeProperty;
      }
      set
      {
        _strokeProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the stroke.
    /// </summary>
    /// <value>The stroke.</value>
    public Brush Stroke
    {
      get
      {
        return _strokeProperty.GetValue() as Brush;
      }
      set
      {
        _strokeProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the stroke thickness property.
    /// </summary>
    /// <value>The stroke thickness property.</value>
    public Property StrokeThicknessProperty
    {
      get
      {
        return _strokeThicknessProperty;
      }
      set
      {
        _strokeThicknessProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the stroke thickness.
    /// </summary>
    /// <value>The stroke thickness.</value>
    public double StrokeThickness
    {
      get
      {
        return (double)_strokeThicknessProperty.GetValue();
      }
      set
      {
        _strokeThicknessProperty.SetValue(value);
      }
    }

    protected virtual void PerformLayout()
    {
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      if (!IsVisible) return;
      if ((Fill != null && _vertexBufferFill == null) ||
           (Stroke != null && _vertexBufferBorder == null) || _performLayout)
      {
        PerformLayout();
        _performLayout = false;
      }

      if (Fill != null)
      {
        GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix;
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        Fill.BeginRender(_vertexBufferFill, _verticesCountFill, PrimitiveType.TriangleFan);
        GraphicsDevice.Device.SetStreamSource(0, _vertexBufferFill, 0);
        GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, _verticesCountFill);
        Fill.EndRender();
      }
      if (Stroke != null && StrokeThickness > 0)
      {
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        Stroke.BeginRender(_vertexBufferBorder, _verticesCountBorder, PrimitiveType.TriangleList);
        GraphicsDevice.Device.SetStreamSource(0, _vertexBufferBorder, 0);
        GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountBorder);
        Stroke.EndRender();
      }

      _lastTimeUsed = SkinContext.Now;
    }

    /// <summary>
    /// Frees this asset.
    /// </summary>
    public override void Free()
    {
      if (_vertexBufferFill != null)
      {
        _vertexBufferFill.Dispose();
        _vertexBufferFill = null;
      }
      if (_vertexBufferBorder != null)
      {
        _vertexBufferBorder.Dispose();
        _vertexBufferBorder = null;
      }
      base.Free();
    }
    /// <summary>
    /// Converts the graphicspath to an array of vertices using trianglefan.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="cx">The cx.</param>
    /// <param name="cy">The cy.</param>
    /// <returns></returns>
    protected VertexBuffer ConvertPathToTriangleFan(GraphicsPath path, float cx, float cy, out PositionColored2Textured[] verts)
    {
      verts = null;
      if (path.PointCount <= 0) return null;
      if (path.PathPoints.Length == 3)
      {
        VertexBuffer vertexBuffer = new VertexBuffer(typeof(PositionColored2Textured), 3, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
        verts = (PositionColored2Textured[])vertexBuffer.Lock(0, 0);

        verts[0].Position = new Vector3(path.PathPoints[0].X, path.PathPoints[0].Y, 1);
        verts[1].Position = new Vector3(path.PathPoints[1].X, path.PathPoints[1].Y, 1);
        verts[2].Position = new Vector3(path.PathPoints[2].X, path.PathPoints[2].Y, 1);
        return vertexBuffer;
      }
      else
      {

        PointF[] points = path.PathPoints;
        int verticeCount = points.Length + 2;

        VertexBuffer vertexBuffer = new VertexBuffer(typeof(PositionColored2Textured), verticeCount, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
        verts = (PositionColored2Textured[])vertexBuffer.Lock(0, 0);

        verts[0].Position = new Vector3(cx, cy, 1);
        verts[1].Position = new Vector3(points[0].X, points[0].Y, 1);
        verts[2].Position = new Vector3(points[1].X, points[1].Y, 1);
        for (int i = 2; i < points.Length; ++i)
        {
          verts[i + 1].Position = new Vector3(points[i].X, points[i].Y, 1);
        }
        verts[verticeCount - 1].Position = new Vector3(points[0].X, points[0].Y, 1);
        return vertexBuffer;
      }
    }

    /// <summary>
    /// Gets the inset.
    /// </summary>
    /// <param name="nextpoint">The nextpoint.</param>
    /// <param name="point">The point.</param>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <param name="thickNess">The thick ness.</param>
    void GetInset(PointF nextpoint, PointF point, out float x, out float y, double thickNessW, double thickNessH, PolygonDirection direction)
    {
      double ang = (float)Math.Atan2((nextpoint.Y - point.Y), (nextpoint.X - point.X));  //returns in radians
      double pi2 = Math.PI / 2.0; //90gr
      if (direction == PolygonDirection.Clockwise)
        ang += pi2;
      else
        ang -= pi2;
      x = (float)(Math.Cos(ang) * thickNessW); //radians
      y = (float)(Math.Sin(ang) * thickNessH);
      _finalLayoutTransform.TransformXY(ref x, ref y);
      x += point.X;
      y += point.Y;
    }

    /// <summary>
    /// Gets the next point.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <param name="i">The i.</param>
    /// <returns></returns>
    PointF GetNextPoint(PointF[] points, int i, int max)
    {
      i++;
      while (i >= max) i -= max;
      return points[i];
    }
    /// <summary>
    /// Converts the graphics path to an array of vertices using trianglestrip.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="cx">The cx.</param>
    /// <param name="cy">The cy.</param>
    /// <param name="thickNess">The thick ness.</param>
    /// <returns></returns>
    protected VertexBuffer ConvertPathToTriangleStrip(GraphicsPath path, float thickNess, bool isClosed, out PositionColored2Textured[] verts)
    {
      if (Name == "path5146")
      {
      }
      verts = null;
      if (path.PointCount <= 0) return null;
      // thickNess /= 2.0f;
      float thicknessW = thickNess;
      float thicknessH = thickNess;
      PolygonDirection direction = PointsDirection(path);
      PointF[] points = path.PathPoints;
      int pointCount = points.Length;
      int verticeCount = (pointCount) * 2 * 3;

      VertexBuffer vertexBuffer = new VertexBuffer(typeof(PositionColored2Textured), verticeCount, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
      verts = (PositionColored2Textured[])vertexBuffer.Lock(0, 0);

      float x, y;
      for (int i = 0; i < (pointCount - 1); ++i)
      {
        int offset = i * 6;
        PointF nextpoint = GetNextPoint(points, i, pointCount);
        GetInset(nextpoint, points[i], out x, out y, (double)thicknessW, (double)thicknessH, direction);
        verts[offset].Position = new Vector3(points[i].X, points[i].Y, 1);
        verts[offset + 1].Position = new Vector3(nextpoint.X, nextpoint.Y, 1);
        verts[offset + 2].Position = new Vector3(x, y, 1);

        verts[offset + 3].Position = new Vector3(nextpoint.X, nextpoint.Y, 1);
        verts[offset + 4].Position = new Vector3(x, y, 1);

        verts[offset + 5].Position = new Vector3(nextpoint.X + (x - points[i].X), nextpoint.Y + (y - points[i].Y), 1);


      }
      return vertexBuffer;
    }


    /// <summary>
    /// Splits the path into triangles.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    protected VertexBuffer Triangulate(GraphicsPath path, float cx, float cy, bool isClosed, out PositionColored2Textured[] verts, out PrimitiveType primitive)
    {
      verts = null;
      if (path.PointCount <= 3)
      {
        primitive = PrimitiveType.TriangleFan;
        return ConvertPathToTriangleFan(path, cx, cy, out verts);
      }
      if (Name == "path134" )
      {
      }
      primitive = PrimitiveType.TriangleList;
      CPolygonShape cutPolygon = new CPolygonShape(path, isClosed);
      cutPolygon.CutEar();

      int count = cutPolygon.NumberOfPolygons;
      VertexBuffer vertexBuffer = new VertexBuffer(typeof(PositionColored2Textured), count * 3, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
      verts = (PositionColored2Textured[])vertexBuffer.Lock(0, 0);
      for (int i = 0; i < count; i++)
      {
        CPoint2D[] triangle = cutPolygon[i];
        int offset = i * 3;
        verts[offset].Position = new Vector3(triangle[0].X, triangle[0].Y, 1);
        verts[offset + 1].Position = new Vector3(triangle[1].X, triangle[1].Y, 1);
        verts[offset + 2].Position = new Vector3(triangle[2].X, triangle[2].Y, 1);
      }
      return vertexBuffer;
    }
    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X);
      layoutRect.Y += (float)(Margin.Y);
      layoutRect.Width -= (float)(Margin.X + Margin.W);
      layoutRect.Height -= (float)(Margin.Y + Margin.Z);
      ActualPosition = new Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      _performLayout = true;
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      base.Arrange(layoutRect);
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(System.Drawing.SizeF availableSize)
    {
      _desiredSize = new System.Drawing.SizeF((float)Width, (float)Height);
      if (Width <= 0)
        _desiredSize.Width = ((float)availableSize.Width) - (float)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = ((float)availableSize.Height) - (float)(Margin.Y + Margin.Z);

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _desiredSize.Width += (float)(Margin.X + Margin.W);
      _desiredSize.Height += (float)(Margin.Y + Margin.Z);
      base.Measure(availableSize);
    }


    protected void ZCross(ref PointF left, ref PointF right, out double result)
    {
      result = left.X * right.Y - left.Y * right.X;
    }
    protected void CalcCentroid(GraphicsPath path, out float cx, out float cy)
    {
      if (path.PointCount == 0)
      {
        cx = 0;
        cy = 0;
        return;
      }
      Vector2 centroid = new Vector2();
      double temp;
      double area = 0;
      PointF v1 = (PointF)path.PathPoints[path.PathPoints.Length - 1];
      PointF v2;
      for (int index = 0; index < path.PathPoints.Length; ++index, v1 = v2)
      {
        v2 = (PointF)path.PathPoints[index];
        ZCross(ref v1, ref v2, out temp);
        area += temp;
        centroid.X += (float)((v1.X + v2.X) * temp);
        centroid.Y += (float)((v1.Y + v2.Y) * temp);
      }
      temp = 1 / (Math.Abs(area) * 3);
      centroid.X *= (float)temp;
      centroid.Y *= (float)temp;

      cx = (float)(Math.Abs(centroid.X));
      cy = (float)(Math.Abs(centroid.Y));
    }
    public PolygonDirection PointsDirection(GraphicsPath points)
    {
      int nCount = 0, j = 0, k = 0;
      int nPoints = points.PointCount;

      if (nPoints < 3)
        return PolygonDirection.Unknown;

      for (int i = 0; i < nPoints - 2; i++)
      {
        j = (i + 1) % nPoints; //j:=i+1;
        k = (i + 2) % nPoints; //k:=i+2;

        double crossProduct = (points.PathPoints[j].X - points.PathPoints[i].X) * (points.PathPoints[k].Y - points.PathPoints[j].Y);
        crossProduct = crossProduct - ((points.PathPoints[j].Y - points.PathPoints[i].Y) * (points.PathPoints[k].X - points.PathPoints[j].X));

        if (crossProduct > 0)
          nCount++;
        else
          nCount--;
      }

      if (nCount < 0)
        return PolygonDirection.Count_Clockwise;
      else if (nCount > 0)
        return PolygonDirection.Clockwise;
      else
        return PolygonDirection.Unknown;
    }
    #region IAsset Members

    /// <summary>
    /// Gets a value indicating the asset is allocated
    /// </summary>
    /// <value><c>true</c> if this asset is allocated; otherwise, <c>false</c>.</value>
    public override bool IsAllocated
    {
      get
      {
        return (_vertexBufferFill != null || _vertexBufferBorder != null || base.IsAllocated);
      }
    }

    /// <summary>
    /// Gets a value indicating whether this asset can be deleted.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this asset can be deleted; otherwise, <c>false</c>.
    /// </value>
    public override bool CanBeDeleted
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


    #endregion
  }
}
