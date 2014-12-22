#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct2D1;
using RectangleF = SharpDX.RectangleF;

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
      Line l = (Line)source;
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
      get { return (double)_x1Property.GetValue(); }
      set { _x1Property.SetValue(value); }
    }

    public AbstractProperty Y1Property
    {
      get { return _y1Property; }
    }

    public double Y1
    {
      get { return (double)_y1Property.GetValue(); }
      set { _y1Property.SetValue(value); }
    }

    public AbstractProperty X2Property
    {
      get { return _x2Property; }
    }

    public double X2
    {
      get { return (double)_x2Property.GetValue(); }
      set { _x2Property.SetValue(value); }
    }

    public AbstractProperty Y2Property
    {
      get { return _y2Property; }
    }

    public double Y2
    {
      get { return (double)_y2Property.GetValue(); }
      set { _y2Property.SetValue(value); }
    }

    protected override Size2F CalculateInnerDesiredSize(Size2F totalSize)
    {
      using (PathGeometry lineRaw = GetLine())
      using (var line = CalculateTransformedPath(lineRaw, new RectangleF(0, 0, totalSize.Width, totalSize.Height)))
      {
        var bounds = line.GetBounds();
        return new Size2F(bounds.Width, bounds.Height);
      }
    }

    protected override void DoPerformLayout(RenderContext context)
    {
      base.DoPerformLayout(context);

      if (Stroke != null && StrokeThickness > 0)
      {
        using (PathGeometry lineRaw = GetLine())
          _geometry.UpdateGeometry(CalculateTransformedPath(lineRaw, _innerRect));
        var bounds = _geometry.OriginalGeom.GetBounds();
        Stroke.SetupBrush(this, ref bounds, context.ZOrder, true);
      }
      //else
      //{
      //  lock (_resourceRenderLock)
      //    TryDispose(ref _geometry);
      //}
    }

    private PathGeometry GetLine()
    {
      PathGeometry path = new PathGeometry(GraphicsDevice11.Instance.Context2D1.Factory);
      using (var sink = path.Open())
      {
        sink.BeginFigure(new Vector2((float)X1, (float)Y1), FigureBegin.Filled);
        sink.AddLine(new Vector2((float)X2, (float)Y2));
        sink.EndFigure(FigureEnd.Open);
        sink.Close();
      }
      return path;
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      base.RenderOverride(localRenderContext);
      var brush = Stroke;
      if (brush != null && StrokeThickness > 0 && _geometry.HasGeom)
      {
        _geometry.UpdateTransform(localRenderContext.Transform);
        GraphicsDevice11.Instance.Context2D1.DrawGeometry(_geometry.TransformedGeom, brush.Brush2D, (float)StrokeThickness);
      }
    }
  }
}
