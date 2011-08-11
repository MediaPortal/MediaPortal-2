#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using System;
using System.Drawing.Drawing2D;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX.Direct3D9;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Line : Shape
  {
    #region Protected fields

    protected AbstractProperty _x1Property;
    protected AbstractProperty _y1Property;
    protected AbstractProperty _x2Property;
    protected AbstractProperty _y2Property;

    #endregion

    #region Ctor

    public Line()
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
      _x1Property = new SProperty(typeof(double), 0.0);
      _y1Property = new SProperty(typeof(double), 0.0);
      _x2Property = new SProperty(typeof(double), 0.0);
      _y2Property = new SProperty(typeof(double), 0.0);
    }

    void Attach()
    {
      _x1Property.Attach(OnCompleteLayoutGetsInvalid);
      _y1Property.Attach(OnCompleteLayoutGetsInvalid);
      _x2Property.Attach(OnCompleteLayoutGetsInvalid);
      _y2Property.Attach(OnCompleteLayoutGetsInvalid);
    }

    void Detach()
    {
      _x1Property.Detach(OnCompleteLayoutGetsInvalid);
      _y1Property.Detach(OnCompleteLayoutGetsInvalid);
      _x2Property.Detach(OnCompleteLayoutGetsInvalid);
      _y2Property.Detach(OnCompleteLayoutGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Line l = (Line) source;
      X1 = l.X1;
      Y1 = l.Y1;
      X2 = l.X2;
      Y2 = l.Y2;
      Attach();
    }

    #endregion

    public AbstractProperty X1Property
    {
      get { return _x1Property; }
    }

    public double X1
    {
      get { return (double) _x1Property.GetValue(); }
      set { _x1Property.SetValue(value); }
    }

    public AbstractProperty Y1Property
    {
      get { return _y1Property; }
    }

    public double Y1
    {
      get { return (double) _y1Property.GetValue(); }
      set { _y1Property.SetValue(value); }
    }

    public AbstractProperty X2Property
    {
      get { return _x2Property; }
    }

    public double X2
    {
      get { return (double) _x2Property.GetValue(); }
      set { _x2Property.SetValue(value); }
    }

    public AbstractProperty Y2Property
    {
      get { return _y2Property; }
    }

    public double Y2
    {
      get { return (double) _y2Property.GetValue(); }
      set { _y2Property.SetValue(value); }
    }

    protected override void DoPerformLayout(RenderContext context)
    {
      base.DoPerformLayout(context);

      if (Stroke != null && StrokeThickness > 0)
      {
        using (GraphicsPath path = GetLine(_innerRect))
        {
          float centerX;
          float centerY;
          TriangulateHelper.CalcCentroid(path, out centerX, out centerY);
          PositionColoredTextured[] verts;
          TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);

          Stroke.SetupBrush(this, ref verts, context.ZOrder, true);
          PrimitiveBuffer.SetPrimitiveBuffer(ref _strokeContext, ref verts, PrimitiveType.TriangleList);
        }
      }
      else
        PrimitiveBuffer.DisposePrimitiveBuffer(ref _strokeContext);
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      using (GraphicsPath p = GetLine(new RectangleF(0, 0, 0, 0)))
      {
        RectangleF bounds = p.GetBounds();

        return new SizeF(bounds.Width, bounds.Height);
      }
    }

    /// <summary>
    /// Get the desired Rounded Rectangle path.
    /// </summary>
    private GraphicsPath GetLine(RectangleF baseRect)
    {
      float x1 = (float) X1;
      float y1 = (float) Y1;
      float x2 = (float) X2;
      float y2 = (float) Y2;

      float w = (float) Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

      float ang = (y2 - y1) / (x2 - x1);
      ang = (float) Math.Atan(ang);
      ang *= (float) (180.0f / Math.PI);
      GraphicsPath mPath = new GraphicsPath();
      System.Drawing.Rectangle r = new System.Drawing.Rectangle((int) x1, (int) y1, (int) w, (int) StrokeThickness);
      mPath.AddRectangle(r);
      mPath.CloseFigure();

      using (Matrix matrix = new Matrix())
      {
        matrix.RotateAt(ang, new PointF(x1, y1), MatrixOrder.Append);
        matrix.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
        mPath.Transform(matrix);
      }
      mPath.Flatten();

      return mPath;
    }
  }
}
