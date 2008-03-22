using System;
using System.Collections.Generic;
using System.Text;
using SlimDX;
using SlimDX.Direct3D;
using System.Drawing;
namespace SkinEngine
{
  public class ColorConverter
  {
    public static ColorValue FromColor(System.Drawing.Color color)
    {
      ColorValue v = new ColorValue(color.A, color.R, color.G, color.B);
      v.Alpha /= 255.0f;
      v.Red /= 255.0f;
      v.Green /= 255.0f;
      v.Blue /= 255.0f;
      return v;
    }

    public static Color FromColor(float a, float r, float g, float b)
    {
      a *= 255;
      r *= 255;
      g *= 255;
      b *= 255;
      return System.Drawing.Color.FromArgb((int)a, (int)r, (int)g, (int)b);
    }
  }
}
