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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Brushes;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine;
using SkinEngine.DirectX;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = SlimDX.Matrix;

namespace SkinEngine.Controls.Visuals
{
  public class Border : Shape
  {
    Property _backgroundProperty;
    Property _borderProperty;
    Property _borderThicknessProperty;
    Property _cornerRadiusProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Border"/> class.
    /// </summary>
    public Border()
    {
      Init();
    }

    public Border(Border b)
      : base(b)
    {
      Init();
      if (b.BorderBrush != null)
        this.BorderBrush = (Brush)b.BorderBrush.Clone();
      if (b.Background != null)
        this.Background = (Brush)b.Background.Clone();
      BorderThickness = b.BorderThickness;
      CornerRadius = b.CornerRadius;
    }

    void Init()
    {
      _borderProperty = new Property(null);
      _backgroundProperty = new Property(null);
      _borderThicknessProperty = new Property((double)1.0);
      _cornerRadiusProperty = new Property((double)0);
      ContentManager.Add(this);

      _borderProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _backgroundProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _borderThicknessProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _cornerRadiusProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    public override object Clone()
    {
      return new Border(this);
    }

    void OnPropertyChanged(Property property)
    {
      Free();
    }

    #region properties
    /// <summary>
    /// Gets or sets the background property.
    /// </summary>
    /// <value>The background property.</value>
    public Property BackgroundProperty
    {
      get
      {
        return _backgroundProperty;
      }
      set
      {
        _backgroundProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the background brush
    /// </summary>
    /// <value>The background.</value>
    public Brush Background
    {
      get
      {
        return _backgroundProperty.GetValue() as Brush;
      }
      set
      {
        _backgroundProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the Border property.
    /// </summary>
    /// <value>The Border property.</value>
    public Property BorderBrushProperty
    {
      get
      {
        return _borderProperty;
      }
      set
      {
        _borderProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the Border brush
    /// </summary>
    /// <value>The Border.</value>
    public Brush BorderBrush
    {
      get
      {
        return _borderProperty.GetValue() as Brush;
      }
      set
      {
        _borderProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the background property.
    /// </summary>
    /// <value>The background property.</value>
    public Property BorderThicknessProperty
    {
      get
      {
        return _borderThicknessProperty;
      }
      set
      {
        _borderThicknessProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the background brush
    /// </summary>
    /// <value>The background.</value>
    public double BorderThickness
    {
      get
      {
        return (double)_borderThicknessProperty.GetValue();
      }
      set
      {
        _borderThicknessProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the background property.
    /// </summary>
    /// <value>The background property.</value>
    public Property CornerRadiusProperty
    {
      get
      {
        return _cornerRadiusProperty;
      }
      set
      {
        _cornerRadiusProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the background brush
    /// </summary>
    /// <value>The background.</value>
    public double CornerRadius
    {
      get
      {
        return (double)_cornerRadiusProperty.GetValue();
      }
      set
      {
        _cornerRadiusProperty.SetValue(value);
      }
    }
    #endregion

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);

      layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
      layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
      layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);

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

    public override void DoRender()
    {
      if (!IsVisible) return;
      if ((Background != null && _vertexBufferFill == null) ||
           (BorderBrush != null && _vertexBufferBorder == null) || _performLayout)
      {
        PerformLayout();
        _performLayout = false;
      }

      ExtendedMatrix m = new ExtendedMatrix(this.Opacity);
      m.Matrix=Matrix.Translation(new Vector3((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualPosition.Z));
      SkinContext.AddTransform(m);
      if (Background != null)
      {
        //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Background.BeginRender(_vertexBufferFill, _verticesCountFill, PrimitiveType.TriangleFan))
        {
          GraphicsDevice.Device.SetStreamSource(0, _vertexBufferFill, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, _verticesCountFill);
          Background.EndRender();
        }
      }
      if (BorderBrush != null && BorderThickness > 0)
      {
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (BorderBrush.BeginRender(_vertexBufferBorder, _verticesCountBorder, PrimitiveType.TriangleList))
        {
          GraphicsDevice.Device.SetStreamSource(0, _vertexBufferBorder, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountBorder);
          BorderBrush.EndRender();
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
      Trace.WriteLine("Border.PerformLayout() " + this.Name);
      Free();
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

      PositionColored2Textured[] verts;
      GraphicsPath path;
      if (Background != null || (BorderBrush != null && BorderThickness > 0))
      {
        using (path = GetRoundedRect(rect, (float)CornerRadius))
        {
          CalcCentroid(path, out centerX, out centerY);
          if (Background != null)
          {
            _vertexBufferFill = ConvertPathToTriangleFan(path, centerX, centerY, out verts);
            if (_vertexBufferFill != null)
            {
              Background.SetupBrush(this, ref verts);


              PositionColored2Textured.Set(_vertexBufferFill, ref verts);
              _verticesCountFill = (verts.Length - 2);
            }
          }

          if (BorderBrush != null && BorderThickness > 0)
          {
            _vertexBufferBorder = ConvertPathToTriangleStrip(path, (float)BorderThickness, true, out verts);
            if (_vertexBufferBorder != null)
            {
              BorderBrush.SetupBrush(this, ref verts);

              PositionColored2Textured.Set(_vertexBufferBorder, ref verts);
              _verticesCountBorder = (verts.Length / 3);
            }

          }
        }
      }
    }




    #region Get the desired Rounded Rectangle path.
    private GraphicsPath GetRoundedRect(RectangleF baseRect, float CornerRadius)
    {
      // if corner radius is less than or equal to zero, 

      // return the original rectangle 

      if (CornerRadius <= 0.0f && CornerRadius <= 0.0f)
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

      if (CornerRadius >= (Math.Min(baseRect.Width, baseRect.Height)) / 2.0)
        return GetCapsule(baseRect);

      // create the arc for the rectangle sides and declare 

      // a graphics path object for the drawing 

      float diameter = CornerRadius * 2.0F;
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
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        mtx.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
      }
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
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        mtx.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
      }
      mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      path.Transform(mtx);
      return path;
    }

    #endregion

  }
}
