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

using System.Windows.Markup;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct3D9;

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

  [ContentProperty("GradientStops")]
  public abstract class GradientBrush : Brush, IAddChild<object>
  {
    #region Protected fields

    protected PositionColoredTextured[] _verts;
    protected AbstractProperty _colorInterpolationModeProperty;
    protected AbstractProperty _gradientStopsProperty;
    protected AbstractProperty _spreadMethodProperty;
    protected AbstractProperty _mappingModeProperty;

    #endregion

    #region Ctor

    protected GradientBrush()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      GradientStops.Dispose();
    }

    void Init()
    {
      _gradientStopsProperty = new SProperty(typeof(GradientStopCollection), new GradientStopCollection(this));
      _colorInterpolationModeProperty = new SProperty(typeof(ColorInterpolationMode),
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
      ColorInterpolationMode = b.ColorInterpolationMode;
      SpreadMethod = b.SpreadMethod;
      MappingMode = b.MappingMode;
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
      get { return (ColorInterpolationMode) _colorInterpolationModeProperty.GetValue(); }
      set { _colorInterpolationModeProperty.SetValue(value); }
    }

    public AbstractProperty GradientStopsProperty
    {
      get { return _gradientStopsProperty; }
    }

    public GradientStopCollection GradientStops
    {
      get { return (GradientStopCollection) _gradientStopsProperty.GetValue(); }
    }

    public AbstractProperty MappingModeProperty
    {
      get { return _mappingModeProperty; }
    }

    public BrushMappingMode MappingMode
    {
      get { return (BrushMappingMode) _mappingModeProperty.GetValue(); }
      set { _mappingModeProperty.SetValue(value); }
    }

    public AbstractProperty SpreadMethodProperty
    {
      get { return _spreadMethodProperty; }
    }

    public GradientSpreadMethod SpreadMethod
    {
      get { return (GradientSpreadMethod) _spreadMethodProperty.GetValue(); }
      set { _spreadMethodProperty.SetValue(value); }
    }

    #endregion

    protected TextureAddress SpreadAddressMode
    {
      get
      {
        switch (SpreadMethod)
        {
          case GradientSpreadMethod.Repeat:
            return TextureAddress.Wrap;
          case GradientSpreadMethod.Reflect:
            return TextureAddress.Mirror;
          //case GradientSpreadMethod.Pad:
          default:
            return TextureAddress.Clamp;
        }
      }
    }


    #region IAddChild Members

    public void AddChild(object o)
    {
      GradientStopCollection gsc = o as GradientStopCollection;
      GradientStop gs = o as GradientStop;
      if (gsc != null)
      {
        GradientStops.Dispose();
        gsc.SetParent(this);
        GradientStopsProperty.SetValue(gsc);
      }
      else if (gs != null)
        GradientStops.Add(gs);
      else if (o != null)
        throw new XamlParserException("Objects of type {0} cannot be added to {1}", o.GetType().Name, GetType().Name);
    }

    #endregion
  }
}
