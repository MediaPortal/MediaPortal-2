#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using SlimDX;
using System.Drawing;

namespace MediaPortal.UI.SkinEngine
{
  public class ColorConverter
  {
    public static Color4 FromColor(Color color)
    {
      Color4 v = new Color4(color.A, color.R, color.G, color.B);
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
      return Color.FromArgb((int)a, (int)r, (int)g, (int)b);
    }
  }
}
