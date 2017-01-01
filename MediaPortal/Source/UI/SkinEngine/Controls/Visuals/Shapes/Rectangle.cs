#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Drawing;
using System.Drawing.Drawing2D;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct3D9;
using RectangleF = SharpDX.RectangleF;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;

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

    void Init()
    {
      _radiusXProperty = new SProperty(typeof(double), 0.0);
      _radiusYProperty = new SProperty(typeof(double), 0.0);
    }

    void Attach()
    {
      _radiusXProperty.Attach(OnArrangeGetsInvalid);
      _radiusYProperty.Attach(OnArrangeGetsInvalid);
    }

    void Detach()
    {
      _radiusXProperty.Detach(OnArrangeGetsInvalid);
      _radiusYProperty.Detach(OnArrangeGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Rectangle r = (Rectangle) source;
      RadiusX = r.RadiusX;
      RadiusY = r.RadiusY;
      Attach();
    }

    #endregion

    public AbstractProperty RadiusXProperty
    {
      get { return _radiusXProperty; }
    }

    public double RadiusX
    {
      get { return (double) _radiusYProperty.GetValue(); }
      set { _radiusYProperty.SetValue(value); }
    }

    public AbstractProperty RadiusYProperty
    {
      get { return _radiusYProperty; }
    }

    public double RadiusY
    {
      get { return (double) _radiusYProperty.GetValue(); }
      set { _radiusYProperty.SetValue(value); }
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      _performLayout = true;
    }

    protected override void DoPerformLayout(RenderContext context)
    {
      base.DoPerformLayout(context);

      // Setup brushes
      if (Fill != null || (Stroke != null && StrokeThickness > 0))
      {
        using (GraphicsPath path = CreateRectanglePath(_innerRect))
        {
          if (path.PointCount == 0)
            return;

          PointF[] pathPoints = path.PathPoints;
          PositionColoredTextured[] verts;

          float centerX = _innerRect.Width / 2 + _innerRect.Left;
          float centerY = _innerRect.Height / 2 + _innerRect.Top;
          if (Fill != null)
          {
            TriangulateHelper.FillPolygon_TriangleList(pathPoints, centerX, centerY, 1, out verts);
            Fill.SetupBrush(this, ref verts, context.ZOrder, true);
            PrimitiveBuffer.SetPrimitiveBuffer(ref _fillContext, ref verts, PrimitiveType.TriangleList);
          }
          else
            PrimitiveBuffer.DisposePrimitiveBuffer(ref _fillContext);

          if (Stroke != null && StrokeThickness > 0)
          {
            TriangulateHelper.TriangulateStroke_TriangleList(pathPoints, (float) StrokeThickness, true, 1, StrokeLineJoin, out verts);
            Stroke.SetupBrush(this, ref verts, context.ZOrder, true);
            PrimitiveBuffer.SetPrimitiveBuffer(ref _strokeContext, ref verts, PrimitiveType.TriangleList);
          }
          else
            PrimitiveBuffer.DisposePrimitiveBuffer(ref _strokeContext);
        }
      }
    }

    protected GraphicsPath CreateRectanglePath(RectangleF rect)
    {
      return GraphicsPathHelper.CreateRoundedRectPath(rect, (float) RadiusX, (float) RadiusY);
    }
  }
}
