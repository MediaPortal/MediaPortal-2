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
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Models.Pictures.PixelOperations
{
  class RedEye : IPixelOperation
  {
    int _tolerance;
    int _saturation;
    public RedEye(int tolerance, int saturation)
    {
      _tolerance=tolerance;
      _saturation = saturation;
    }
    public Color ProcessPixel( Color color)
    {
      int tolerence = _tolerance;
      double setSaturation = (double)_saturation / ((double)100);

      // The higher the saturation, the more red it is
      int saturation = GetSaturation(color);

      // The higher the difference between the other colors, the more red it is
      int difference = color.R - Math.Max(color.B, color.G);

      // If it is within tolerence, and the saturation is high
      if ((difference > tolerence) && (saturation > 100))
      {
        double i = 255.0 * GetIntensity(color);
        byte ib = (byte)(i * setSaturation); // adjust the red color for user inputted saturation
        return Color.FromArgb(color.A, ib, (byte)color.G, (byte)color.B);
      }
      else
      {
        return color;
      }
    }

    private int GetSaturation(Color color)
    {
      double min;
      double max;
      double delta;

      double r = (double)color.R / 255;
      double g = (double)color.G / 255;
      double b = (double)color.B / 255;

      double s;

      min = Math.Min(Math.Min(r, g), b);
      max = Math.Max(Math.Max(r, g), b);
      delta = max - min;

      if (max == 0 || delta == 0)
      {
        // R, G, and B must be 0, or all the same.
        // In this case, S is 0, and H is undefined.
        // Using H = 0 is as good as any...
        s = 0;
      }
      else
      {
        s = delta / max;
      }

      return (int)(s * 255);
    }
    public double GetIntensity(Color color)
    {
      return ((0.114 * (double)color.B) + (0.587 * (double)color.G) + (0.299 * (double)color.R)) / 255.0;
    }
  }
}
