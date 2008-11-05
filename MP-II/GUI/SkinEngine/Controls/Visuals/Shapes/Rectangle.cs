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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Rendering;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals.Shapes
{
  public class Rectangle : Shape
  {
    #region Private fields

    Property _radiusXProperty;
    Property _radiusYProperty;

    #endregion

    #region Ctor

    public Rectangle()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _radiusXProperty = new Property(typeof(double), 0.0);
      _radiusYProperty = new Property(typeof(double), 0.0);
    }

    void Attach()
    {
      _radiusXProperty.Attach(OnRadiusChanged);
      _radiusYProperty.Attach(OnRadiusChanged);
    }

    void Detach()
    {
      _radiusXProperty.Detach(OnRadiusChanged);
      _radiusYProperty.Detach(OnRadiusChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Rectangle r = (Rectangle) source;
      RadiusX = copyManager.GetCopy(r.RadiusX);
      RadiusY = copyManager.GetCopy(r.RadiusY);
      Attach();
    }

    #endregion

    void OnRadiusChanged(Property property)
    {
      Invalidate();
      if (Screen != null) Screen.Invalidate(this);
    }

    public Property RadiusXProperty
    {
      get { return _radiusXProperty; }
    }

    public double RadiusX
    {
      get { return (double)_radiusYProperty.GetValue(); }
      set { _radiusYProperty.SetValue(value); }
    }

    public Property RadiusYProperty
    {
      get { return _radiusYProperty; }
    }

    public double RadiusY
    {
      get { return (double)_radiusYProperty.GetValue(); }
      set { _radiusYProperty.SetValue(value); }
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("Rectangle.Arrange :{0} X {1},Y {2} W {3}xH {4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      ComputeInnerRectangle(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      Initialize();
      InitializeTriggers();
      IsInvalidLayout = false;

      _performLayout = true;

      if (Screen != null)
        Screen.Invalidate(this);
    }

    protected override void PerformLayout()
    {
      double w = ActualWidth;
      double h = ActualHeight;
      SizeF rectSize = new SizeF((float)w, (float)h);

      //Trace.WriteLine(String.Format("Rectangle.PerformLayout")); 

      ExtendedMatrix m = new ExtendedMatrix();
      if (_finalLayoutTransform != null)
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
      PositionColored2Textured[] verts;
      if (Fill != null || (Stroke != null && StrokeThickness > 0))
      {
        GraphicsPath path;
        using (path = GetRoundedRect(rect, (float)RadiusX, (float)RadiusY))
        {
          float centerX = rect.Width / 2 + rect.Left;
          float centerY = rect.Height / 2 + rect.Top;
          //CalcCentroid(path, out centerX, out centerY);
          if (Fill != null)
          {
            if (SkinContext.UseBatching == false)
            {
              if (_fillAsset == null)
              {
                _fillAsset = new VisualAssetContext("Rectangle._fillContext:" + Name);
                ContentManager.Add(_fillAsset);
              }
              _fillAsset.VertexBuffer = ConvertPathToTriangleFan(path, centerX, centerY, out verts);
              if (_fillAsset.VertexBuffer != null)
              {
                Fill.SetupBrush(this, ref verts);

                PositionColored2Textured.Set(_fillAsset.VertexBuffer, ref verts);
                _verticesCountFill = (verts.Length / 3);
              }
            }
            else
            {
              PathToTriangleList(path, centerX, centerY, out verts);
              _verticesCountFill = (verts.Length / 3);
              Fill.SetupBrush(this, ref verts);
              if (_fillContext == null)
              {
                _fillContext = new PrimitiveContext(_verticesCountFill, ref verts);
                Fill.SetupPrimitive(_fillContext);
                RenderPipeline.Instance.Add(_fillContext);
              }
              else
              {
                _fillContext.OnVerticesChanged(_verticesCountFill, ref verts);
              }
            }
          }

          if (Stroke != null && StrokeThickness > 0)
          {
            if (SkinContext.UseBatching == false)
            {
              if (_borderAsset == null)
              {
                _borderAsset = new VisualAssetContext("Rectangle._borderContext:" + Name);
                ContentManager.Add(_borderAsset);
              }
              using (path = GetRoundedRect(rect, (float)RadiusX, (float)RadiusY))
              {
                _borderAsset.VertexBuffer = ConvertPathToTriangleStrip(path, (float)StrokeThickness, true, GeometryUtility.PolygonDirection.Clockwise, out verts, _finalLayoutTransform, false);
                if (_borderAsset.VertexBuffer != null)
                {

                  Stroke.SetupBrush(this, ref verts);

                  PositionColored2Textured.Set(_borderAsset.VertexBuffer, ref verts);
                  _verticesCountBorder = (verts.Length / 3);
                }
              }
            }
            else
            {
              StrokePathToTriangleStrip(path, (float)StrokeThickness, true, out verts, _finalLayoutTransform);
              _verticesCountBorder = (verts.Length / 3);
              Stroke.SetupBrush(this, ref verts);
              if (_strokeContext == null)
              {
                _strokeContext = new PrimitiveContext(_verticesCountBorder, ref verts);
                Stroke.SetupPrimitive(_strokeContext);
                RenderPipeline.Instance.Add(_strokeContext);
              }
              else
              {
                _strokeContext.OnVerticesChanged(_verticesCountBorder, ref verts);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Get the desired Rounded Rectangle path.
    /// </summary>
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
        if (_finalLayoutTransform != null)
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
      GraphicsPath path = new GraphicsPath();

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
      if (_finalLayoutTransform != null)
        mtx.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      path.Transform(mtx);

      path.Flatten();
      return path;
    }

    /// <summary>
    /// Gets the desired Capsular path.
    /// </summary>
    private GraphicsPath GetCapsule(RectangleF baseRect)
    {
      RectangleF arc;
      GraphicsPath path = new GraphicsPath();
      try
      {
        float diameter;
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
      if (_finalLayoutTransform != null)
        mtx.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      path.Transform(mtx);
      return path;
    }
  }
}
