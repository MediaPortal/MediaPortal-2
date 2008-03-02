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
using SkinEngine.Rendering;

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
      //Trace.WriteLine("Line.PerformLayout() " + this.Name);

      double w = ActualWidth;
      double h = ActualHeight;
      float centerX, centerY;
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
      System.Drawing.RectangleF rect = new System.Drawing.RectangleF(0, 0, rectSize.Width, rectSize.Height);
      rect.X += (float)ActualPosition.X;
      rect.Y += (float)ActualPosition.Y;
      //Fill brush
      GraphicsPath path;
      PositionColored2Textured[] verts;

      //border brush

      if (Stroke != null && StrokeThickness > 0)
      {
        using (path = GetLine(rect))
        {
          CalcCentroid(path, out centerX, out centerY);
          if (_borderAsset == null)
          {
            _borderAsset = new VisualAssetContext("Line._borderContext:" + this.Name);
            ContentManager.Add(_borderAsset);
          }
          if (SkinContext.UseBatching == false)
          {
            _borderAsset.VertexBuffer = ConvertPathToTriangleFan(path, centerX, centerY, out verts);
            if (_borderAsset.VertexBuffer != null)
            {
              Stroke.SetupBrush(this, ref verts);
              PositionColored2Textured.Set(_borderAsset.VertexBuffer, ref verts);
              _verticesCountBorder = (verts.Length / 3);
            }
          }
          else
          {
            Shape.PathToTriangleList(path, centerX, centerY, out verts);
            _verticesCountBorder = (verts.Length / 3);
            Stroke.SetupBrush(this, ref verts);
            if (_borderContext == null)
            {
              _borderContext = new PrimitiveContext(_verticesCountBorder, ref verts);
              Stroke.SetupPrimitive(_borderContext);
              RenderPipeline.Instance.Add(_borderContext);
            }
            else
            {
              _borderContext.OnVerticesChanged(_verticesCountBorder, ref verts);
            }
          }
        }
      }

    }
    /*
    /// <summary>
    /// Renders the visual
    /// </summary>
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
    }*/
    public override void Measure(SizeF availableSize)
    {
      using (GraphicsPath p = GetLine(new RectangleF(0, 0, 0, 0)))
      {
        RectangleF bounds = p.GetBounds();
        if (Width > 0) bounds.Width = (float)Width;
        if (Height > 0) bounds.Height = (float)Height;
        bounds.Width *= SkinContext.Zoom.Width;
        bounds.Height *= SkinContext.Zoom.Height;

        float marginWidth = (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
        float marginHeight = (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
        _desiredSize = new System.Drawing.SizeF((float)bounds.Width, (float)bounds.Height);
        if (availableSize.Width > 0 && Width <= 0)
          _desiredSize.Width = (float)(availableSize.Width - marginWidth);
        if (availableSize.Width > 0 && Height <= 0)
          _desiredSize.Height = (float)(availableSize.Height - marginHeight);

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
        _desiredSize.Width += marginWidth;
        _desiredSize.Height += marginHeight;

        _availableSize = new SizeF(availableSize.Width, availableSize.Height);
        //Trace.WriteLine(String.Format("line.measure :{0} {1}x{2} returns {3}x{4}", this.Name, (int)availableSize.Width, (int)availableSize.Height, (int)_desiredSize.Width, (int)_desiredSize.Height));
      }
    }

    #region Get the desired Rounded Rectangle path.
    private GraphicsPath GetLine(RectangleF baseRect)
    {
      float x1 = (float)(X1);
      float y1 = (float)(Y1);
      float x2 = (float)(X2);
      float y2 = (float)(Y2);

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
    #endregion

  }
}
