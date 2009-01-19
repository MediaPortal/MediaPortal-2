#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.SkinEngine.MarkupExtensions
{
  public class ColorMarkupExtension: IEvaluableMarkupExtension
  {
    #region Protected fields

    protected Color? _baseColor = null;

    // RGB members
    protected int _r = -1;
    protected int _g = -1;
    protected int _b = -1;
    protected int _rdiff = -1;
    protected int _gdiff = -1;
    protected int _bdiff = -1;
    protected bool _rgbUsage = false;

    // HSL members
    protected float _h = -1;
    protected float _s = -1;
    protected float _v = -1;
    protected float _hdiff = -1;
    protected float _sdiff = -1;
    protected float _vdiff = -1;
    protected bool _hsvUsage = false;

    protected int _a = -1;
    protected int _adiff = -1;

    #endregion

    public ColorMarkupExtension() { }

    #region Properties

    public Color? BaseColor
    {
      get { return _baseColor; }
      set { _baseColor = value; }
    }

    public int R
    {
      get { return _r; }
      set
      {
        RGBUsage();
        _r = value;
      }
    }

    public int G
    {
      get { return _g; }
      set
      {
        RGBUsage();
        _g = value;
      }
    }

    public int B
    {
      get { return _b; }
      set
      {
        RGBUsage();
        _b = value;
      }
    }

    public int Rdiff
    {
      get { return _rdiff; }
      set
      {
        RGBUsage();
        _rdiff = value;
      }
    }

    public int Gdiff
    {
      get { return _gdiff; }
      set
      {
        RGBUsage();
        _gdiff = value;
      }
    }

    public int Bdiff
    {
      get { return _bdiff; }
      set
      {
        RGBUsage();
        _bdiff = value;
      }
    }

    public float H
    {
      get { return _h; }
      set
      {
        HSVUsage();
        _h = value;
      }
    }

    public float S
    {
      get { return _s; }
      set
      {
        HSVUsage();
        _s = value;
      }
    }

    public float V
    {
      get { return _v; }
      set
      {
        HSVUsage();
        _v = value;
      }
    }

    public float Hdiff
    {
      get { return _hdiff; }
      set
      {
        HSVUsage();
        _hdiff = value;
      }
    }

    public float Sdiff
    {
      get { return _sdiff; }
      set
      {
        HSVUsage();
        _sdiff = value;
      }
    }

    public float Vdiff
    {
      get { return _vdiff; }
      set
      {
        HSVUsage();
        _vdiff = value;
      }
    }

    public bool IsRGBUsage
    {
      get { return _rgbUsage; }
    }

    public bool IsHSVUsage
    {
      get { return _hsvUsage; }
    }

    public int A
    {
      get { return _a; }
      set { _a = value; }
    }

    public int Adiff
    {
      get { return _adiff; }
      set { _adiff = value; }
    }

    #endregion

    #region Protected methods

    protected void RGBUsage()
    {
      if (_hsvUsage)
        throw new InvalidStateException("The Color markup extension supports either using the RGB properties OR using the HSL properties");
      _rgbUsage = true;
    }

    protected void HSVUsage()
    {
      if (_rgbUsage)
        throw new InvalidStateException("The Color markup extension supports either using the RGB properties OR using the HSL properties");
      _hsvUsage = true;
    }

    protected static Color CreateColor(byte a, float r, float g, float b)
    {
      return Color.FromArgb(a, (byte) r, (byte) g, (byte) b);
    }

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
          return CreateColor(a, v, t, p);
        case 1:
          return CreateColor(a, q, v, p);
        case 2:
          return CreateColor(a, p, v, t);
        case 3:
          return CreateColor(a, p, q, v);
        case 4:
          return CreateColor(a, t, p, v);
        case 5:
          return CreateColor(a, v, p, q);
        default:
          throw new Exception("Error in color algorithm");
      }
    }

    protected static Color FromAhsl(byte a, float h, float s, float l)
    {
      // Algorithm from Wikipedia (http://en.wikipedia.org/wiki/HSV_color_space), 2009-01-19
      // hsl -> rgb
      float q;
      if (l < 0.5)
        q = l * (1 + s);
      else
        q = l + s - l * s;
      float p = 2 * l - q;
      float hk = h / 360;
      float[] t = { hk + 1f/3, hk, hk - 1f/3 };
      float[] c = new float[3];
      for (int i=0; i<3; i++)
      {
        if (t[i] < 0)
          t[i] += 1;
        if (t[i] > 1)
          t[i] -= 1;
        if (t[i] < 1f/6)
          c[i] = p + ((q - p)*6*t[i]);
        else if (t[i] < 1f/2)
          c[i] = q;
        else if (t[i] < 2f/3)
          c[i] = p + ((q - p)*6*(2f/3 - t[i]));
        else
          c[i] = p;
      }
      return CreateColor(a, c[0], c[1], c[2]);
    }

    protected static void GetHSV(Color color, out float h, out float s, out float v)
    {
      byte r = color.R;
      byte g = color.G;
      byte b = color.B;
      byte min = Math.Min(Math.Min(r, g), b);
      byte max = Math.Max(Math.Max(r, g), b);
      if (max == min)
        h = 0;
      else if (max == r)
        h = (60*(g - b)/(float) (max - min)) % 360;
      else if (max == g)
        h = 60*(b - r)/(float) (max - min) + 120;
      else /* max == b */
        h = 60*(r - g)/(float) (max - min) + 240;
      if (max == 0)
        s = 0;
      else
        s = 1 - min/(float) max;
      v = max;
    }

    protected Color CalculateColor()
    {
      Color? color = _baseColor ?? Color.Black;
      byte a = color.Value.A;
      if (_a != -1)
        a = (byte) _a;
      if (_adiff != -1)
        a += (byte) _adiff;
      if (_rgbUsage)
      {
        byte r = color.Value.R;
        byte g = color.Value.G;
        byte b = color.Value.B;
        if (_r != -1)
          r = (byte) _r;
        if (_g != -1)
          g = (byte) _g;
        if (_b != -1)
          b = (byte) _b;
        if (_rdiff != -1)
          r += (byte) _rdiff;
        if (_gdiff != -1)
          g += (byte) _gdiff;
        if (_bdiff != -1)
          b += (byte) _bdiff;
        return Color.FromArgb(a, r, g, b);
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
        if (_hdiff != -1)
          h += _hdiff;
        if (_sdiff != -1)
          s += _sdiff;
        if (_vdiff != -1)
          v += _vdiff;
        return FromAhsv(a, h, s, v);
      }
      return Color.FromArgb(a, color.Value);
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
