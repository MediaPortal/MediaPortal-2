#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Drawing;
using System.Drawing.Drawing2D;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Rectangle : Shape
  {
    #region Protected fields

    protected AbstractProperty _radiusXProperty;
    protected AbstractProperty _radiusYProperty;

    #endregion

    #region Ctor

    public Rectangle()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    void Init()
    {
      _radiusXProperty = new SProperty(typeof(double), 0.0);
      _radiusYProperty = new SProperty(typeof(double), 0.0);
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

    void OnRadiusChanged(AbstractProperty property, object oldValue)
    {
      Invalidate();
      if (Screen != null) Screen.Invalidate(this);
    }

    public AbstractProperty RadiusXProperty
    {
      get { return _radiusXProperty; }
    }

    public double RadiusX
    {
      get { return (double)_radiusYProperty.GetValue(); }
      set { _radiusYProperty.SetValue(value); }
    }

    public AbstractProperty RadiusYProperty
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
      RemoveMargin(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      Initialize();
      InitializeTriggers();

      _performLayout = true;

      if (Screen != null)
        Screen.Invalidate(this);
    }

    protected override void PerformLayout()
    {
      if (!_performLayout)
        return;
      base.PerformLayout();
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
        using (path = CreateRectanglePath(rect))
        {
          if (path.PointCount == 0)
            return;
          float centerX = rect.Width / 2 + rect.Left;
          float centerY = rect.Height / 2 + rect.Top;
          //CalcCentroid(path, out centerX, out centerY);
          if (Fill != null)
          {
            if (SkinContext.UseBatching)
            {
              TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
              _verticesCountFill = verts.Length / 3;
              Fill.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);
              if (_fillContext == null)
              {
                _fillContext = new PrimitiveContext(_verticesCountFill, ref verts);
                Fill.SetupPrimitive(_fillContext);
                RenderPipeline.Instance.Add(_fillContext);
              }
              else
                _fillContext.OnVerticesChanged(_verticesCountFill, ref verts);
            }
            else
            {
              if (_fillAsset == null)
              {
                _fillAsset = new VisualAssetContext("Rectangle._fillContext:" + Name, Screen.Name);
                ContentManager.Add(_fillAsset);
              }
              TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
              if (verts != null)
              {
                _fillAsset.VertexBuffer = PositionColored2Textured.Create(verts.Length);
                Fill.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);

                PositionColored2Textured.Set(_fillAsset.VertexBuffer, ref verts);
                _verticesCountFill = (verts.Length / 3);
              }
            }
          }

          if (Stroke != null && StrokeThickness > 0)
          {
            if (SkinContext.UseBatching == false)
            {
              if (_borderAsset == null)
              {
                _borderAsset = new VisualAssetContext("Rectangle._borderContext:" + Name, Screen.Name);
                ContentManager.Add(_borderAsset);
              }
              using (path = CreateRectanglePath(rect))
              {
                TriangulateHelper.TriangulateStroke_TriangleList(path, (float) StrokeThickness, true, PolygonDirection.Clockwise, out verts, _finalLayoutTransform);
                if (verts != null)
                {
                  _borderAsset.VertexBuffer = PositionColored2Textured.Create(verts.Length);
                  Stroke.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);

                  PositionColored2Textured.Set(_borderAsset.VertexBuffer, ref verts);
                  _verticesCountBorder = (verts.Length / 3);
                }
              }
            }
            else
            {
              TriangulateHelper.TriangulateStroke_TriangleList(path, (float)StrokeThickness, true, out verts, _finalLayoutTransform);
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
          }
        }
      }
    }

    protected GraphicsPath CreateRectanglePath(RectangleF rect)
    {
      ExtendedMatrix layoutTransform = _finalLayoutTransform ?? new ExtendedMatrix();
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        layoutTransform = layoutTransform.Multiply(em);
      }
      return GraphicsPathHelper.CreateRoundedRectPath(rect, (float) RadiusX, (float) RadiusY, layoutTransform);
    }
  }
}
