#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Drawing;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Markup extension calculating color values, based on another color or given by absolute RGB or HSV
  /// attributes. The color can also be calculated using offset values for R, G, B or H, S, V values.
  /// Only one color scheme can be used, either RGB or HSV, that means don't set both RGB and HSV values,
  /// else an exception will be thrown.
  /// </summary>
  public class ColorMarkupExtension: IEvaluableMarkupExtension
  {
    #region Protected fields

    protected Color? _baseColor = null;

    // RGB members
    protected int _r = -1;
    protected int _g = -1;
    protected int _b = -1;
    protected int _rdiff = 0;
    protected int _gdiff = 0;
    protected int _bdiff = 0;
    protected bool _rgbUsage = false;

    // HSL members
    protected float _h = -1;
    protected float _s = -1;
    protected float _v = -1;
    protected float _hdiff = 0;
    protected float _sdiff = 0;
    protected float _vdiff = 0;
    protected bool _hsvUsage = false;

    protected int _a = -1;
    protected int _adiff = 0;

    #endregion

    public ColorMarkupExtension() { }

    #region Properties

    /// <summary>
    /// Use this property to set the color all color calculations are based on.
    /// If this property isn't set, the base color will default to <see cref="Color.Black"/>.
    /// </summary>
    public Color? BaseColor
    {
      get { return _baseColor; }
      set { _baseColor = value; }
    }

    /// <summary>
    /// Gets or sets the R value explicitly. This property overrides the R value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from 0 to 255.
    /// </summary>
    public int R
    {
      get { return _r; }
      set
      {
        CheckValue(value, 0, 255, "R");
        RGBUsage();
        _r = value;
      }
    }

    /// <summary>
    /// Gets or sets the G value explicitly. This property overrides the R value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from 0 to 255.
    /// </summary>
    public int G
    {
      get { return _g; }
      set
      {
        CheckValue(value, 0, 255, "G");
        RGBUsage();
        _g = value;
      }
    }

    /// <summary>
    /// Gets or sets the B value explicitly. This property overrides the R value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from 0 to 255.
    /// </summary>
    public int B
    {
      get { return _b; }
      set
      {
        CheckValue(value, 0, 255, "B");
        RGBUsage();
        _b = value;
      }
    }

    /// <summary>
    /// Gets or sets the R offset. The value of this property will be added to the R value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -255 to 255.
    /// </summary>
    public int Rdiff
    {
      get { return _rdiff; }
      set
      {
        CheckValue(value, -255, 255, "Rdiff");
        RGBUsage();
        _rdiff = value;
      }
    }

    /// <summary>
    /// Gets or sets the G offset. The value of this property will be added to the G value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -255 to 255.
    /// </summary>
    public int Gdiff
    {
      get { return _gdiff; }
      set
      {
        CheckValue(value, -255, 255, "Gdiff");
        RGBUsage();
        _gdiff = value;
      }
    }

    /// <summary>
    /// Gets or sets the B offset. The value of this property will be added to the B value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -255 to 255.
    /// </summary>
    public int Bdiff
    {
      get { return _bdiff; }
      set
      {
        CheckValue(value, -255, 255, "Bdiff");
        RGBUsage();
        _bdiff = value;
      }
    }

    /// <summary>
    /// Gets or sets the H value explicitly. This property overrides the H value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -360 to 360.
    /// </summary>
    public float H
    {
      get { return _h; }
      set
      {
        CheckValue(value, -360f, 360f, "H");
        HSVUsage();
        _h = value;
      }
    }

    /// <summary>
    /// Gets or sets the S value explicitly. This property overrides the S value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -1 to 1.
    /// </summary>
    public float S
    {
      get { return _s; }
      set
      {
        CheckValue(value, -1f, 1f, "S");
        HSVUsage();
        _s = value;
      }
    }

    /// <summary>
    /// Gets or sets the V value explicitly. This property overrides the V value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -1 to 1.
    /// </summary>
    public float V
    {
      get { return _v; }
      set
      {
        CheckValue(value, -1f, 1f, "V");
        HSVUsage();
        _v = value;
      }
    }

    /// <summary>
    /// Gets or sets the H offset. The value of this property will be added to the H value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -360 to 360.
    /// </summary>
    public float Hdiff
    {
      get { return _hdiff; }
      set
      {
        CheckValue(value, -360f, 360f, "Hdiff");
        HSVUsage();
        _hdiff = value;
      }
    }

    /// <summary>
    /// Gets or sets the S offset. The value of this property will be added to the S value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -1 to 1.
    /// </summary>
    public float Sdiff
    {
      get { return _sdiff; }
      set
      {
        CheckValue(value, -1f, 1f, "Sdiff");
        HSVUsage();
        _sdiff = value;
      }
    }

    /// <summary>
    /// Gets or sets the V offset. The value of this property will be added to the V value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -1 to 1.
    /// </summary>
    public float Vdiff
    {
      get { return _vdiff; }
      set
      {
        CheckValue(value, -1f, 1f, "Vdiff");
        HSVUsage();
        _vdiff = value;
      }
    }

    /// <summary>
    /// Returns the information if the RGB color scheme is used. This property will get automtically
    /// set if one of the RGB properties gets set.
    /// </summary>
    public bool IsRGBUsage
    {
      get { return _rgbUsage; }
    }

    /// <summary>
    /// Returns the information if the HSV color scheme is used. This property will get automtically
    /// set if one of the HSV properties gets set.
    /// </summary>
    public bool IsHSVUsage
    {
      get { return _hsvUsage; }
    }

    /// <summary>
    /// Gets or sets the A value explicitly. This property overrides the A value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from 0 to 255.
    /// </summary>
    public int A
    {
      get { return _a; }
      set
      {
        CheckValue(value, 0, 255, "A");
        _a = value;
      }
    }

    /// <summary>
    /// Gets or sets the A offset. The value of this property will be added to the A value of the
    /// <see cref="BaseColor"/>.
    /// The value must be in the range from -255 to 255.
    /// </summary>
    public int Adiff
    {
      get { return _adiff; }
      set
      {
        CheckValue(value, -255, 255, "Adiff");
        _adiff = value;
      }
    }

    #endregion

    #region Protected methods

    protected static void CheckValue<T>(T value, T min, T max, string propertyName) where T: IComparable
    {
      if (min.CompareTo(value) == 1 || max.CompareTo(value) == -1)
        throw new ArgumentOutOfRangeException(
            string.Format("ColorMarkupExtension: Illegal value '{0}' for property {1}, the value should be between {2} and {3}", value, propertyName, min, max));
    }

    protected void RGBUsage()
    {
      if (_hsvUsage)
        throw new IllegalCallException("The Color markup extension supports either using the RGB properties OR using the HSL properties");
      _rgbUsage = true;
    }

    protected void HSVUsage()
    {
      if (_rgbUsage)
        throw new IllegalCallException("The Color markup extension supports either using the RGB properties OR using the HSL properties");
      _hsvUsage = true;
    }

    /// <summary>
    /// Clips the specified float to a byte value between 0 and 255.
    /// </summary>
    /// <param name="x">Float value which might be lower than 0 or greater than 255.</param>
    /// <returns>Clipped byte value.</returns>
    protected static byte ClipToByte(float x)
    {
      return (byte) Math.Max(Math.Min(255, x), 0);
    }

    /// <summary>
    /// Scales a float between 0 and 1 to a byte value between 0 and 255.
    /// </summary>
    /// <param name="x">Float value between 0 and 1.</param>
    /// <returns>Scaled byte value.</returns>
    protected static byte ScaleToByte(float x)
    {
      return (byte) (x*255);
    }

    /// <summary>
    /// Calculates the returned RGB <see cref="Color"/> instance from the specified HSV values and the
    /// specified A value.
    /// </summary>
    /// <param name="a">A value.</param>
    /// <param name="h">H value.</param>
    /// <param name="s">S value.</param>
    /// <param name="v">V value.</param>
    /// <returns>Translated color.</returns>
    protected static Color FromAhsv(byte a, float h, float s, float v)
    {
      // Algorithm from Wikipedia (http://en.wikipedia.org/wiki/HSV_color_space), 2009-01-19
      // hsv -> rgb
      int hi = (int) Math.Floor(h/60f)%6;
      float f = (h/60) - (float) Math.Floor(h/60);
      float p = v*(1 - s);
      float q = v*(1 - f*s);
      float t = v*(1 - (1 - f)*s);
      switch (hi)
      {
        case 0:
          return Color.FromArgb(a, ScaleToByte(v), ScaleToByte(t), ScaleToByte(p));
        case 1:
          return Color.FromArgb(a, ScaleToByte(q), ScaleToByte(v), ScaleToByte(p));
        case 2:
          return Color.FromArgb(a, ScaleToByte(p), ScaleToByte(v), ScaleToByte(t));
        case 3:
          return Color.FromArgb(a, ScaleToByte(p), ScaleToByte(q), ScaleToByte(v));
        case 4:
          return Color.FromArgb(a, ScaleToByte(t), ScaleToByte(p), ScaleToByte(v));
        case 5:
          return Color.FromArgb(a, ScaleToByte(v), ScaleToByte(p), ScaleToByte(q));
        default:
          throw new Exception("Error in color algorithm");
      }
    }

    /// <summary>
    /// Calculates HSV values from the specified RGB <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The color value to be converted into the HSV color space.</param>
    /// <param name="h">Return value H e [0, 360).</param>
    /// <param name="s">Return value S e [0, 1].</param>
    /// <param name="v">Return value V e [0, 1].</param>
    protected static void GetHSV(Color color, out float h, out float s, out float v)
    {
      // Algorithm from Wikipedia (http://en.wikipedia.org/wiki/HSV_color_space), 2009-01-19
      // rgb -> hsv
      float r = color.R/255f;
      float g = color.G/255f;
      float b = color.B/255f;
      float min = Math.Min(Math.Min(r, g), b);
      float max = Math.Max(Math.Max(r, g), b);
      if (max == min)
        h = 0;
      else if (max == r)
        h = (60*(g - b)/(max - min)) % 360;
      else if (max == g)
        h = 60*(b - r)/(max - min) + 120;
      else /* max == b */
        h = 60*(r - g)/(max - min) + 240;
      if (max == 0)
        s = 0;
      else
        s = 1 - min/max;
      v = max;
    }

    /// <summary>
    /// Does the actual color calculation based on the properties.
    /// </summary>
    /// <returns>Calculated color.</returns>
    protected Color CalculateColor()
    {
      Color? color = _baseColor ?? Color.Black;
      int a = color.Value.A;
      if (_a != -1)
        a = _a;
      a += _adiff;
      if (_rgbUsage)
      {
        int r = color.Value.R;
        int g = color.Value.G;
        int b = color.Value.B;
        if (_r != -1)
          r = (byte) _r;
        if (_g != -1)
          g = (byte) _g;
        if (_b != -1)
          b = (byte) _b;
        r += _rdiff;
        g += _gdiff;
        b += _bdiff;
        return Color.FromArgb(ClipToByte(a), ClipToByte(r), ClipToByte(g), ClipToByte(b));
      }
      if (_hsvUsage)
      {
        float h;
        float s;
        float v;
        GetHSV(color.Value, out h, out s, out v);
        if (_h != -1)
          h = _h;
        if (_s != -1)
          s = _s;
        if (_v != -1)
          v = _v;
        h += _hdiff;
        s += _sdiff;
        v += _vdiff;
        return FromAhsv(ClipToByte(a), h, s, v);
      }
      return Color.FromArgb(ClipToByte(a), color.Value);
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    object IEvaluableMarkupExtension.Evaluate(IParserContext context)
    {
      return CalculateColor();
    }

    #endregion
  }
}
