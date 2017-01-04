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

using System.Drawing;

namespace MediaPortal.UI.SkinEngine.Utils
{
  public class CalcHelper
  {
    /// <summary>
    /// Tests whether any of the rect's bounds is <see cref="float.NaN"/>.
    /// </summary>
    /// <param name="rect">The rect to test.</param>
    /// <returns><c>true</c> if none of the rect's bounds is <see cref="float.NaN"/>. <c>false</c>, if any
    /// of its bounds is <see cref="float.NaN"/>.</returns>
    public static bool HasExtends(RectangleF rect)
    {
      return !rect.IsEmpty && !float.IsNaN(rect.Top) && !float.IsNaN(rect.Bottom) && !float.IsNaN(rect.Left) && !float.IsNaN(rect.Right);
    }

    /// <summary>
    /// Tests whether any of the rect's bounds is <see cref="float.NaN"/>.
    /// </summary>
    /// <param name="rect">The rect to test.</param>
    /// <returns><c>true</c> if none of the rect's bounds is <see cref="float.NaN"/>. <c>false</c>, if any
    /// of its bounds is <see cref="float.NaN"/>.</returns>
    public static bool HasExtends(Rectangle rect)
    {
      return !rect.IsEmpty && !double.IsNaN(rect.Top) && !double.IsNaN(rect.Bottom) && !double.IsNaN(rect.Left) && !double.IsNaN(rect.Right);
    }

    public static void Bound(ref int value, int lowerBound, int upperBound)
    {
      if (value < lowerBound)
        value = lowerBound;
      if (value > upperBound)
        value = upperBound;
    }

    public static void LowerBound(ref int value, int lowerBound)
    {
      if (value < lowerBound)
        value = lowerBound;
    }

    public static void UpperBound(ref int value, int upperBound)
    {
      if (value > upperBound)
        value = upperBound;
    }
  }
}
