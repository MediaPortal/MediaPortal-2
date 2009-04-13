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
using System.Drawing.Drawing2D;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.DirectX.Triangulate;
using MediaPortal.SkinEngine.Rendering;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals.Shapes
{
  public class Line : Shape
  {
    #region Private fields

    Property _x1Property;
    Property _y1Property;
    Property _x2Property;
    Property _y2Property;

    #endregion

    #region Ctor

    public Line()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _x1Property = new Property(typeof(double), 0.0);
      _y1Property = new Property(typeof(double), 0.0);
      _x2Property = new Property(typeof(double), 0.0);
      _y2Property = new Property(typeof(double), 0.0);
    }

    void Attach()
    {
      _x1Property.Attach(OnCoordinateChanged);
      _y1Property.Attach(OnCoordinateChanged);
      _x2Property.Attach(OnCoordinateChanged);
      _y2Property.Attach(OnCoordinateChanged);
    }

    void Detach()
    {
      _x1Property.Detach(OnCoordinateChanged);
      _y1Property.Detach(OnCoordinateChanged);
      _x2Property.Detach(OnCoordinateChanged);
      _y2Property.Detach(OnCoordinateChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Line l = (Line) source;
      X1 = copyManager.GetCopy(l.X1);
      Y1 = copyManager.GetCopy(l.Y1);
      X2 = copyManager.GetCopy(l.X2);
      Y2 = copyManager.GetCopy(l.Y2);
      Attach();
    }

    #endregion

    void OnCoordinateChanged(Property property, object oldValue)
    {
      Invalidate();
      if (Screen != null) Screen.Invalidate(this);
    }

    public Property X1Property
    {
      get { return _x1Property; }
    }

    public double X1
    {
      get { return (double) _x1Property.GetValue(); }
      set { _x1Property.SetValue(value); }
    }

    public Property Y1Property
    {
      get { return _y1Property; }
    }

    public double Y1
    {
      get { return (double) _y1Property.GetValue(); }
      set { _y1Property.SetValue(value); }
    }

    public Property X2Property
    {
      get { return _x2Property; }
    }

    public double X2
    {
      get { return (double) _x2Property.GetValue(); }
      set { _x2Property.SetValue(value); }
    }

    public Property Y2Property
    {
      get { return _y2Property; }
    }

    public double Y2
    {
      get { return (double) _y2Property.GetValue(); }
      set { _y2Property.SetValue(value); }
    }

    protected override void PerformLayout()
    {
      if (!_performLayout)
        return;
      base.PerformLayout();
      //Trace.WriteLine("Line.PerformLayout() " + this.Name);

      double w = ActualWidth;
      double h = ActualHeight;
      SizeF rectSize = new SizeF((float)w, (float)h);

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix *= _finalLayoutTransform.Matrix;
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        m.Matrix *= em.Matrix;
      }
      m.InvertSize(ref rectSize);
      RectangleF rect = new RectangleF(0, 0, rectSize.Width, rectSize.Height);
      rect.X += ActualPosition.X;
      rect.Y += ActualPosition.Y;
      //Fill brush
      PositionColored2Textured[] verts;

      //border brush

      if (Stroke != null && StrokeThickness > 0)
      {
        using (GraphicsPath path = GetLine(rect))
        {
          float centerX;
          float centerY;
          TriangulateHelper.CalcCentroid(path, out centerX, out centerY);
          if (_borderAsset == null)
          {
            _borderAsset = new VisualAssetContext("Line._borderContext:" + Name);
            ContentManager.Add(_borderAsset);
          }
          if (SkinContext.UseBatching)
          {
            TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
            _verticesCountBorder = (verts.Length / 3);
            Stroke.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);
            if (_strokeContext == null)
            {
              _strokeContext = new PrimitiveContext(_verticesCountBorder, ref verts);
              Stroke.SetupPrimitive(_strokeContext);
              RenderPipeline.Instance.Add(_strokeContext);
            }
            else
              _strokeContext.OnVerticesChanged(_verticesCountBorder, ref verts);
          }
          else
          {
            TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
            if (verts != null)
            {
              _borderAsset.VertexBuffer = PositionColored2Textured.Create(verts.Length);
              Stroke.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);
              PositionColored2Textured.Set(_borderAsset.VertexBuffer, ref verts);
              _verticesCountBorder = (verts.Length / 3);
            }
          }
        }
      }

    }

    // Not needed as the drawing will be done by Shape class
    /*
    public override void DoRender()
    {
      if (!IsVisible) return;
      if (Stroke == null) return;

      if ((_borderAsset != null && !_borderAsset.IsAllocated) || _borderAsset == null)
        _performLayout = true;
      if (_performLayout)
      {
        PerformLayout();
        _performLayout = false;
      }
      SkinContext.AddOpacity(this.Opacity);
      //ExtendedMatrix m = new ExtendedMatrix();
      //m.Matrix = Matrix.Translation(new Vector3((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualPosition.Z));
      //SkinContext.AddTransform(m);
      if (_borderAsset != null)
      {
        //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Stroke.BeginRender(_borderAsset.VertexBuffer, _verticesCountBorder, PrimitiveType.TriangleList))
        {
          GraphicsDevice.Device.SetStreamSource(0, _borderAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountBorder);
          Stroke.EndRender();
        }
        _borderAsset.LastTimeUsed = SkinContext.Now;
      }
      //SkinContext.RemoveTransform();
      SkinContext.RemoveOpacity();
    }
    */

    public override void Measure(ref SizeF totalSize)
    {
      using (GraphicsPath p = GetLine(new RectangleF(0, 0, 0, 0)))
      {
        RectangleF bounds = p.GetBounds();

        _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);

        if (double.IsNaN(Width))
          _desiredSize.Width = bounds.Width * SkinContext.Zoom.Width;

        if (double.IsNaN(Height))
          _desiredSize.Height = bounds.Height * SkinContext.Zoom.Height;


        if (LayoutTransform != null)
        {
          ExtendedMatrix m;
          LayoutTransform.GetTransform(out m);
          SkinContext.AddLayoutTransform(m);
        }
        SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

        if (LayoutTransform != null)
          SkinContext.RemoveLayoutTransform();

        totalSize = _desiredSize;
        AddMargin(ref totalSize);

        //Trace.WriteLine(String.Format("line.measure :{0} returns {1}x{2}", this.Name, (int)_desiredSize.Width, (int)_desiredSize.Height));
      }
    }

    /// <summary>
    /// Get the desired Rounded Rectangle path.
    /// </summary>
    private GraphicsPath GetLine(RectangleF baseRect)
    {
      float x1 = (float)(X1);
      float y1 = (float)(Y1);
      float x2 = (float)(X2);
      float y2 = (float)(Y2);

      float w = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

      float ang = (y2 - y1) / (x2 - x1);
      ang = (float) Math.Atan(ang);
      ang *= (float) (180.0f / Math.PI);
      GraphicsPath mPath = new GraphicsPath();
      System.Drawing.Rectangle r = new System.Drawing.Rectangle((int)x1, (int)y1, (int)w, (int)StrokeThickness);
      mPath.AddRectangle(r);
      mPath.CloseFigure();

      Matrix matrix = new Matrix();
      matrix.RotateAt(ang, new PointF(x1, y1), MatrixOrder.Append);


      if (_finalLayoutTransform != null)
        matrix.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      matrix.Scale(SkinContext.Zoom.Width, SkinContext.Zoom.Height, MatrixOrder.Append);

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
  }
}
