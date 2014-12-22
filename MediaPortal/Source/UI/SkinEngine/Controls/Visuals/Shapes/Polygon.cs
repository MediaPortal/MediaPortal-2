#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using SharpDX.Direct2D1;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Polygon : Shape
  {
    #region Protected fields

    protected AbstractProperty _pointsProperty;

    #endregion

    #region Ctor

    public Polygon()
    {
      Init();
    }

    void Init()
    {
      _pointsProperty = new SProperty(typeof(PointCollection), new PointCollection());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Polygon p = (Polygon)source;
      Points = new PointCollection(p.Points);
    }

    #endregion

    #region Public properties

    public AbstractProperty PointsProperty
    {
      get { return _pointsProperty; }
    }

    public PointCollection Points
    {
      get { return (PointCollection)_pointsProperty.GetValue(); }
      set { _pointsProperty.SetValue(value); }
    }

    #endregion

    #region Layouting

    protected override void DoPerformLayout(RenderContext context)
    {
      // Setup brushes
      if (Fill != null || (Stroke != null && StrokeThickness > 0))
      {
        using(PathGeometry pathRaw = GetPolygon())
        {
          lock (_resourceRenderLock)
            _geometry.UpdateGeometry(CalculateTransformedPath(pathRaw, _innerRect));
          var boundaries = _geometry.OriginalGeom.GetBounds();
          var fill = Fill;
          if (fill != null && !_fillDisabled)
            fill.SetupBrush(this, ref boundaries, context.ZOrder, true);

          var stroke = Stroke;
          if (stroke != null)
            stroke.SetupBrush(this, ref boundaries, context.ZOrder, true);
        }
      }
      //else
      //{
      //  lock (_resourceRenderLock)
      //    TryDispose(ref _geometry);
      //}
      base.DoPerformLayout(context);
    }

    protected override Size2F CalculateInnerDesiredSize(Size2F totalSize)
    {
      using (PathGeometry pathRaw = GetPolygon())
      using (var p = CalculateTransformedPath(pathRaw, new RectangleF(0, 0, 0, 0)))
      {
        var bounds = p.GetBounds();
        return new Size2F(bounds.Width, bounds.Height);
      }
    }

    /// <summary>
    /// Get the desired Polygon path.
    /// </summary>
    private PathGeometry GetPolygon()
    {
      var points = Points.ToArray();
      if (points.Length < 3)
        return null;
      PathGeometry path = new PathGeometry(GraphicsDevice11.Instance.Context2D1.Factory);
      using (var sink = path.Open())
      {
        sink.BeginFigure(points[0], FigureBegin.Filled);

        for (int i = 1; i < points.Length; i++)
        {
          sink.AddLine(points[0]);
        }

        sink.EndFigure(FigureEnd.Closed);
        sink.Close();
      }
      return path;
    }

    #endregion
  }
}
