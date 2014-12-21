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

using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Ellipse : Shape
  {
    protected override void DoPerformLayout(RenderContext context)
    {
      base.DoPerformLayout(context);

      // Setup brushes
      if (Fill != null || (Stroke != null && StrokeThickness > 0))
      {
        var ellipse = new SharpDX.Direct2D1.Ellipse {RadiusX = _innerRect.Width / 2, RadiusY = _innerRect.Height /2, Point = _innerRect.Center};
        _geometry = new EllipseGeometry(GraphicsDevice11.Instance.RenderTarget2D.Factory, ellipse);
        var fill = Fill;
        if (fill != null)
          fill.SetupBrush(this, ref _innerRect, context.ZOrder, true);

        var stroke = Stroke;
        if (stroke != null)
          stroke.SetupBrush(this, ref _innerRect, context.ZOrder, true);
      }
      else
      {
        TryDispose(ref _geometry);
      }
    }
  }
}
