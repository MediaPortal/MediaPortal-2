#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using SharpDX.Mathematics.Interop;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public class LinearGradientBrush : GradientBrush
  {
    #region Protected fields

    protected AbstractProperty _startPointProperty;
    protected AbstractProperty _endPointProperty;

    #endregion

    #region Ctor

    public LinearGradientBrush()
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
      _startPointProperty = new SProperty(typeof(Vector2), new Vector2(0.0f, 0.0f));
      _endPointProperty = new SProperty(typeof(Vector2), new Vector2(1.0f, 1.0f));
    }

    void Attach()
    {
      _startPointProperty.Attach(OnPropertyChanged);
      _endPointProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _startPointProperty.Detach(OnPropertyChanged);
      _endPointProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      LinearGradientBrush b = (LinearGradientBrush)source;
      StartPoint = copyManager.GetCopy(b.StartPoint);
      EndPoint = copyManager.GetCopy(b.EndPoint);
      _refresh = true;
      Attach();
    }

    #endregion

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      base.OnPropertyChanged(prop, oldValue);
      _refresh = true;
      UpdateBrush();
    }

    protected override void OnImmutableResourcePropertyChanged(AbstractProperty prop, object oldValue)
    {
      base.OnPropertyChanged(prop, oldValue);
      _refresh = true;
    }

    /// <summary>
    /// Updates properties of existing brush that can be changed. For immutable properties (DX resources) a full recreation is required.
    /// </summary>
    protected void UpdateBrush()
    {
      // Forward all property changes to internal brush
      var brush = _brush2D as SharpDX.Direct2D1.LinearGradientBrush;
      if (brush != null)
      {
        brush.StartPoint = StartPoint;
        brush.EndPoint = EndPoint;
        _refresh = false; // We could update an existing brush, no need to recreate it
      }
    }

    protected override void OnRelativeTransformChanged(IObservable trans)
    {
      _refresh = true;
      base.OnRelativeTransformChanged(trans);
    }

    public AbstractProperty StartPointProperty
    {
      get { return _startPointProperty; }
    }

    public Vector2 StartPoint
    {
      get { return (Vector2)_startPointProperty.GetValue(); }
      set { _startPointProperty.SetValue(value); }
    }

    public AbstractProperty EndPointProperty
    {
      get { return _endPointProperty; }
    }

    public Vector2 EndPoint
    {
      get { return (Vector2)_endPointProperty.GetValue(); }
      set { _endPointProperty.SetValue(value); }
    }

    public override void SetupBrush(FrameworkElement parent, ref RawRectangleF boundary, float zOrder, bool adaptVertsToBrushTexture)
    {
      base.SetupBrush(parent, ref boundary, zOrder, adaptVertsToBrushTexture);
      _refresh = true;
    }

    public override void Allocate()
    {
      base.Allocate();
      LinearGradientBrushProperties props = new LinearGradientBrushProperties
      {
        StartPoint = StartPoint,
        EndPoint = EndPoint
      };
      SetBrush(new SharpDX.Direct2D1.LinearGradientBrush(GraphicsDevice11.Instance.Context2D1, props, GradientStops.GradientStopCollection2D));
    }
  }
}
