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
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX;
using SharpDX.Direct2D1;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class RadialGradientBrush : GradientBrush
  {
    #region Protected fields

    protected AbstractProperty _centerProperty;
    protected AbstractProperty _gradientOriginProperty;
    protected AbstractProperty _radiusXProperty;
    protected AbstractProperty _radiusYProperty;

    #endregion

    #region Ctor

    public RadialGradientBrush()
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
      _centerProperty = new SProperty(typeof(Vector2), new Vector2(0.5f, 0.5f));
      _gradientOriginProperty = new SProperty(typeof(Vector2), new Vector2(0.5f, 0.5f));
      _radiusXProperty = new SProperty(typeof(double), 0.5);
      _radiusYProperty = new SProperty(typeof(double), 0.5);
    }

    void Attach()
    {
      _centerProperty.Attach(OnPropertyChanged);
      _gradientOriginProperty.Attach(OnPropertyChanged);
      _radiusXProperty.Attach(OnPropertyChanged);
      _radiusYProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _centerProperty.Detach(OnPropertyChanged);
      _gradientOriginProperty.Detach(OnPropertyChanged);
      _radiusXProperty.Detach(OnPropertyChanged);
      _radiusYProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      RadialGradientBrush b = (RadialGradientBrush)source;
      Center = copyManager.GetCopy(b.Center);
      GradientOrigin = copyManager.GetCopy(b.GradientOrigin);
      RadiusX = b.RadiusX;
      RadiusY = b.RadiusY;
      _refresh = true;
      Attach();
    }

    #endregion

    protected override void OnRelativeTransformChanged(IObservable trans)
    {
      _refresh = true;
      base.OnRelativeTransformChanged(trans);
    }

    #region Protected methods

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      _refresh = true;
      UpdateBrush();
      base.OnPropertyChanged(prop, oldValue);
    }

    protected void UpdateBrush()
    {
      // Forward all property changes to internal brush
      var brush = _brush2D as SharpDX.Direct2D1.RadialGradientBrush;
      if (brush != null)
      {
        brush.Center = TransformToBoundary(Center);
        brush.RadiusX = TransformRadiusX(RadiusX);
        brush.RadiusY = TransformRadiusX(RadiusY);
        brush.GradientOriginOffset = TransformOffset(GradientOrigin);
        _refresh = false; // We could update an existing brush, no need to recreate it
      }
    }

    #endregion

    #region Public properties

    public AbstractProperty CenterProperty
    {
      get { return _centerProperty; }
    }

    public Vector2 Center
    {
      get { return (Vector2)_centerProperty.GetValue(); }
      set { _centerProperty.SetValue(value); }
    }

    public AbstractProperty GradientOriginProperty
    {
      get { return _gradientOriginProperty; }
    }

    public Vector2 GradientOrigin
    {
      get { return (Vector2)_gradientOriginProperty.GetValue(); }
      set { _gradientOriginProperty.SetValue(value); }
    }

    public AbstractProperty RadiusXProperty
    {
      get { return _radiusXProperty; }
    }

    public double RadiusX
    {
      get { return (double)_radiusXProperty.GetValue(); }
      set { _radiusXProperty.SetValue(value); }
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

    #endregion

    #region Public methods

    public override void SetupBrush(FrameworkElement parent, ref RectangleF boundary, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref boundary, zOrder, adaptVertsToBrushTexture);
      _refresh = true;
    }

    public override void Allocate()
    {
      base.Allocate();
      RadialGradientBrushProperties props = new RadialGradientBrushProperties
      {
        Center = TransformToBoundary(Center),
        RadiusX = TransformRadiusX(RadiusX),
        RadiusY = TransformRadiusY(RadiusY),
        GradientOriginOffset = TransformOffset(GradientOrigin)
      };
      _brush2D = new SharpDX.Direct2D1.RadialGradientBrush(GraphicsDevice11.Instance.Context2D1, props, GradientStops.GradientStopCollection2D);
    }

    protected float TransformRadiusX(double radiusX)
    {
      return (float)(_vertsBounds.Width * radiusX);
    }

    protected float TransformRadiusY(double radiusY)
    {
      return (float)(_vertsBounds.Height * radiusY);
    }

    // TODO: check this logic, results looks different compared to before
    protected Vector2 TransformOffset(Vector2 relativeCoord)
    {
      var x = _vertsBounds.Width * (relativeCoord.X - 0.5f); // Relative to center
      var y = _vertsBounds.Height * (relativeCoord.Y - 0.5f); // Relative to center
      return new Vector2(x, y);
    }

    #endregion
  }
}
