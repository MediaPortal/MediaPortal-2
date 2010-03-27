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

using MediaPortal.Core.General;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public enum BrushMappingMode
  {
    Absolute,
    RelativeToBoundingBox
  };

  public enum ColorInterpolationMode
  {
    ColorInterpolationModeScRgbLinearInterpolation,
    ColorInterpolationModeSRgbLinearInterpolation
  };

  public enum GradientSpreadMethod
  {
    Pad,
    Reflect,
    Repeat
  };

  public class GradientBrush : Brush, IAddChild<GradientStop>
  {
    #region Private fields

    protected PositionColored2Textured[] _verts;
    AbstractProperty _colorInterpolationModeProperty;
    AbstractProperty _gradientStopsProperty;
    AbstractProperty _spreadMethodProperty;
    AbstractProperty _mappingModeProperty;

    #endregion

    #region Ctor

    public GradientBrush()
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
      _gradientStopsProperty = new SProperty(typeof(GradientStopCollection), new GradientStopCollection(this));
      _colorInterpolationModeProperty =
        new SProperty(typeof(ColorInterpolationMode),
                     ColorInterpolationMode.ColorInterpolationModeScRgbLinearInterpolation);
      _spreadMethodProperty = new SProperty(typeof(GradientSpreadMethod), GradientSpreadMethod.Pad);
      _mappingModeProperty = new SProperty(typeof(BrushMappingMode), BrushMappingMode.RelativeToBoundingBox);
    }

    void Attach()
    {
      _gradientStopsProperty.Attach(OnPropertyChanged);
      _colorInterpolationModeProperty.Attach(OnPropertyChanged);
      _spreadMethodProperty.Attach(OnPropertyChanged);
      _mappingModeProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _gradientStopsProperty.Detach(OnPropertyChanged);
      _colorInterpolationModeProperty.Detach(OnPropertyChanged);
      _spreadMethodProperty.Detach(OnPropertyChanged);
      _mappingModeProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      GradientBrush b = (GradientBrush) source;
      ColorInterpolationMode = copyManager.GetCopy(b.ColorInterpolationMode);
      SpreadMethod = copyManager.GetCopy(b.SpreadMethod);
      MappingMode = copyManager.GetCopy(b.MappingMode);
      foreach (GradientStop stop in b.GradientStops)
        GradientStops.Add(copyManager.GetCopy(stop));
      Attach();
    }

    #endregion

    /// <summary>
    /// Called when one of the gradients changed.
    /// </summary>
    public void OnGradientsChanged()
    {
      OnPropertyChanged(_gradientStopsProperty, null);
    }

    #region Public properties

    public AbstractProperty ColorInterpolationModeProperty
    {
      get { return _colorInterpolationModeProperty; }
    }

    public ColorInterpolationMode ColorInterpolationMode
    {
      get { return (ColorInterpolationMode)_colorInterpolationModeProperty.GetValue(); }
      set { _colorInterpolationModeProperty.SetValue(value); }
    }

    public AbstractProperty GradientStopsProperty
    {
      get { return _gradientStopsProperty; }
    }

    public GradientStopCollection GradientStops
    {
      get { return (GradientStopCollection)_gradientStopsProperty.GetValue(); }
    }

    public AbstractProperty MappingModeProperty
    {
      get { return _mappingModeProperty; }
    }

    public BrushMappingMode MappingMode
    {
      get { return (BrushMappingMode)_mappingModeProperty.GetValue(); }
      set { _mappingModeProperty.SetValue(value); }
    }

    public AbstractProperty SpreadMethodProperty
    {
      get { return _spreadMethodProperty; }
    }

    public GradientSpreadMethod SpreadMethod
    {
      get { return (GradientSpreadMethod)_spreadMethodProperty.GetValue(); }
      set { _spreadMethodProperty.SetValue(value); }
    }

    #endregion

    #region Protected methods

    protected void SetColor(VertexBuffer vertexbuffer)
    {
      Color4 color = ColorConverter.FromColor(GradientStops[0].Color);
      color.Alpha *= (float)Opacity;
      for (int i = 0; i < _verts.Length; ++i)
      {
        _verts[i].Color = color.ToArgb();
      }

      PositionColored2Textured.Set(vertexbuffer, ref _verts);
    }

    #endregion

    #region IAddChild Members

    public void AddChild(GradientStop o)
    {
      GradientStops.Add(o);
    }

    #endregion
  }
}
