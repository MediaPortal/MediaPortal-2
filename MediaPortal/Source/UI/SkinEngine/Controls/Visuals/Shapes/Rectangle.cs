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
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct2D1;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Rectangle : Shape
  {
    #region Protected fields

    protected AbstractProperty _radiusXProperty;
    protected AbstractProperty _radiusYProperty;
    protected RoundedRectangle _roundedRectangle;

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
        var roundedRectangle = new RoundedRectangle { RadiusX = (float)RadiusX, RadiusY = (float)RadiusY, Rect = _innerRect };
        _geometry = new RoundedRectangleGeometry(GraphicsDevice11.Instance.RenderTarget2D.Factory, roundedRectangle);
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
