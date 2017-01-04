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

using SharpDX;

namespace MediaPortal.UI.SkinEngine
{
  public static class ColorConverter
  {
    /// <summary>
    /// Converts a given <paramref name="color"/> into a <see cref="Color4"/> where all componets are using relative values (normalized to 1.00).
    /// </summary>
    /// <param name="color">Color.</param>
    /// <returns>Color4.</returns>
    public static Color4 FromColor(Color color)
    {
      Color4 v = new Color4(color.R, color.G, color.B, color.A);
      v.Alpha /= 255.0f;
      v.Red /= 255.0f;
      v.Green /= 255.0f;
      v.Blue /= 255.0f;
      return v;
    }

    /// <summary>
    /// Creates a <see cref="Color"/> from relative values (normalized to 1.00).
    /// </summary>
    /// <param name="a">Alpha.</param>
    /// <param name="r">Red.</param>
    /// <param name="g">Green.</param>
    /// <param name="b">Blue.</param>
    /// <returns>Color</returns>
    public static Color FromColor(float a, float r, float g, float b)
    {
      a *= 255;
      r *= 255;
      g *= 255;
      b *= 255;
      return FromArgb((int)a, (int)r, (int)g, (int)b);
    }

    /// <summary>
    /// Converts the given <paramref name="value"/> into a <see cref="Color"/> and returns it as <paramref name="color"/>.
    /// It supports <c>Color</c> and <see cref="string"/> (color names, #RRGGBB or #AARRGGBB) input values.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <param name="color">Outputs color.</param>
    /// <returns><c>true if successful.</c></returns>
    public static bool ConvertColor(object value, out Color color)
    {
      if (value is Color)
      {
        color = (Color)value;
        return true;
      }
      if (value is System.Drawing.Color)
      {
        color = ((System.Drawing.Color)value).FromDrawingColor();
        return true;
      }
      var convertFrom = new System.Drawing.ColorConverter().ConvertFrom(value);
      if (convertFrom == null)
      {
        color = new Color();
        return false;
      }
      color = ((System.Drawing.Color)convertFrom).FromDrawingColor();
      return true;
    }
    public static Color FromArgb(int alpha, Color color)
    {
      return new Color(color.R, color.G, color.B, alpha);
    }

    public static Color FromDrawingColor(this System.Drawing.Color color)
    {
      return new Color(color.R, color.G, color.B, color.A);
    }

    public static Color FromArgb(int alpha, int r, int g, int b)
    {
      return new Color(r, g, b, alpha);
    }
  }
}
