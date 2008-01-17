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
  public class Rectangle : Shape
  {

    Property _radiusXProperty;
    Property _radiusYProperty;

    public Rectangle()
    {
      Init();
    }

    public Rectangle(Rectangle s)
      : base(s)
    {
      Init();
      RadiusX = s.RadiusX;
      RadiusY = s.RadiusY;
    }

    public override object Clone()
    {
      return new Rectangle(this);
    }

    void Init()
    {
      _radiusXProperty = new Property(0.0);
      _radiusYProperty = new Property(0.0);
      _radiusXProperty.Attach(new PropertyChangedHandler(OnRadiusChanged));
      _radiusYProperty.Attach(new PropertyChangedHandler(OnRadiusChanged));
    }

    void OnRadiusChanged(Property property)
    {
      Invalidate();
    }


    /// <summary>
    /// Gets or sets the fill property.
    /// </summary>
    /// <value>The fill property.</value>
    public Property RadiusXProperty
    {
      get
      {
        return _radiusXProperty;
      }
      set
      {
        _radiusXProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the radius X.
    /// </summary>
    /// <value>The radius X.</value>
    public double RadiusX
    {
      get
      {
        return (double)_radiusYProperty.GetValue();
      }
      set
      {
        _radiusYProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the radius Y property.
    /// </summary>
    /// <value>The radius Y property.</value>
    public Property RadiusYProperty
    {
      get
      {
        return _radiusYProperty;
      }
      set
      {
        _radiusYProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the radius Y.
    /// </summary>
    /// <value>The radius Y.</value>
    public double RadiusY
    {
      get
      {
        return (double)_radiusYProperty.GetValue();
      }
      set
      {
        _radiusYProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X);
      layoutRect.Y += (float)(Margin.Y);
      layoutRect.Width -= (float)(Margin.X + Margin.W);
      layoutRect.Height -= (float)(Margin.Y + Margin.Z);
      ActualPosition = new Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (!IsArrangeValid)
      {
        IsArrangeValid = true;
        InitializeBindings();
        InitializeTriggers();
      }
      _isLayoutInvalid = false;

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      }
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      if (!IsVisible) return;
      if ((Fill != null && _vertexBufferFill == null) ||
           (Stroke != null && _vertexBufferBorder == null && StrokeThickness>0) || _performLayout)
      {
        PerformLayout();
        _performLayout = false;
      }

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix.Translation(new Vector3((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualPosition.Z));
      SkinContext.AddTransform(m);
      if (Fill != null)
      {
        //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Fill.BeginRender(_vertexBufferFill, _verticesCountFill, PrimitiveType.TriangleFan))
        {
          GraphicsDevice.Device.SetStreamSource(0, _vertexBufferFill, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, _verticesCountFill);
          Fill.EndRender();
        }
      }
      if (Stroke != null && StrokeThickness > 0)
      {
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Stroke.BeginRender(_vertexBufferBorder, _verticesCountBorder, PrimitiveType.TriangleList))
        {
          GraphicsDevice.Device.SetStreamSource(0, _vertexBufferBorder, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountBorder);
          Stroke.EndRender();
        }
      }

      SkinContext.RemoveTransform();
      _lastTimeUsed = SkinContext.Now;
    }


    /// <summary>
    /// Performs the layout.
    /// </summary>
    protected override void PerformLayout()
    {
      Trace.WriteLine("Rectangle.PerformLayout() " + this.Name + "  " + this._performLayout);
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
      System.Drawing.RectangleF rect = new System.Drawing.RectangleF(0, 0, rectSize.Width, rectSize.Height);

      PositionColored2Textured[] verts;
      GraphicsPath path;
      if (Fill != null || (Stroke != null && StrokeThickness > 0))
      {
        using (path = GetRoundedRect(rect, (float)RadiusX, (float)RadiusY))
        {
          CalcCentroid(path, out centerX, out centerY);
          if (Fill != null)
          {
            _vertexBufferFill = ConvertPathToTriangleFan(path, centerX, centerY, out verts);
            if (_vertexBufferFill != null)
            {
              Fill.SetupBrush(this, ref verts);

              PositionColored2Textured.Set(_vertexBufferFill, ref verts);
              _verticesCountFill = (verts.Length - 2);
            }
          }

          if (Stroke != null && StrokeThickness > 0)
          {
            using (path = GetRoundedRect(rect, (float)RadiusX, (float)RadiusY))
            {
              _vertexBufferBorder = ConvertPathToTriangleStrip(path, (float)StrokeThickness, true, out verts);
              if (_vertexBufferBorder != null)
              {

                Stroke.SetupBrush(this, ref verts);

                PositionColored2Textured.Set(_vertexBufferBorder, ref verts);
                _verticesCountBorder = (verts.Length / 3);
              }
            }
          }
        }
      }
    }

    #region Get the desired Rounded Rectangle path.
    private GraphicsPath GetRoundedRect(RectangleF baseRect, float radiusX, float radiusY)
    {
      // if corner radius is less than or equal to zero, 

      // return the original rectangle 

      if (radiusX <= 0.0f && radiusY <= 0.0f)
      {
        GraphicsPath mPath = new GraphicsPath();
        mPath.AddRectangle(baseRect);
        mPath.CloseFigure();
        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
        m.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
        m.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
        if (LayoutTransform != null)
        {
          ExtendedMatrix em;
          LayoutTransform.GetTransform(out em);
          m.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
        }
        m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
        mPath.Transform(m);
        mPath.Flatten();
        return mPath;
      }

      // if the corner radius is greater than or equal to 

      // half the width, or height (whichever is shorter) 

      // then return a capsule instead of a lozenge 

      if (radiusX >= (Math.Min(baseRect.Width, baseRect.Height)) / 2.0)
        return GetCapsule(baseRect);

      // create the arc for the rectangle sides and declare 

      // a graphics path object for the drawing 

      float diameter = radiusX * 2.0F;
      SizeF sizeF = new SizeF(diameter, diameter);
      RectangleF arc = new RectangleF(baseRect.Location, sizeF);
      GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();

      // top left arc 


      path.AddArc(arc, 180, 90);

      // top right arc 

      arc.X = baseRect.Right - diameter;
      path.AddArc(arc, 270, 90);

      // bottom right arc 

      arc.Y = baseRect.Bottom - diameter;
      path.AddArc(arc, 0, 90);

      // bottom left arc

      arc.X = baseRect.Left;
      path.AddArc(arc, 90, 90);

      path.CloseFigure();
      System.Drawing.Drawing2D.Matrix mtx = new System.Drawing.Drawing2D.Matrix();
      mtx.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
      mtx.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      path.Transform(mtx);

      path.Flatten();
      return path;
    }
    #endregion

    #region Gets the desired Capsular path.
    private GraphicsPath GetCapsule(RectangleF baseRect)
    {
      float diameter;
      RectangleF arc;
      GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
      try
      {
        if (baseRect.Width > baseRect.Height)
        {
          // return horizontal capsule 

          diameter = baseRect.Height;
          SizeF sizeF = new SizeF(diameter, diameter);
          arc = new RectangleF(baseRect.Location, sizeF);
          path.AddArc(arc, 90, 180);
          arc.X = baseRect.Right - diameter;
          path.AddArc(arc, 270, 180);
        }
        else if (baseRect.Width < baseRect.Height)
        {
          // return vertical capsule 

          diameter = baseRect.Width;
          SizeF sizeF = new SizeF(diameter, diameter);
          arc = new RectangleF(baseRect.Location, sizeF);
          path.AddArc(arc, 180, 180);
          arc.Y = baseRect.Bottom - diameter;
          path.AddArc(arc, 0, 180);
        }
        else
        {
          // return circle 

          path.AddEllipse(baseRect);
        }
      }
      catch (Exception)
      {
        path.AddEllipse(baseRect);
      }
      finally
      {
        path.CloseFigure();
      }
      System.Drawing.Drawing2D.Matrix mtx = new System.Drawing.Drawing2D.Matrix();
      mtx.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
      mtx.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      path.Transform(mtx);
      return path;
    }
    #endregion

  }
}
