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

using System;

namespace MediaPortal.UI.SkinEngine.Utils
{
  public static class DoubleUtils
  {
    internal const double DBL_EPSILON = 2.2204460492503131e-016; //smallest value where 1 + DBLE_EPSILON != 1

    /// <summary>
    /// Returns whether the double is 'close' to zero.
    /// </summary>
    /// <param name="value">The double to compare.</param>
    /// <returns>True if the double to compare is close to zero.</returns>
    public static bool IsZero(double value)
    {
      return Math.Abs(value) < 10.0 * DBL_EPSILON;
    }

    /// <summary>
    /// Returns whether the double is 'close' to one.
    /// </summary>
    /// <param name="value">The double to compare.</param>
    /// <returns>True if the double to compare is close to one.</returns>
    public static bool IsOne(double value)
    {
      return Math.Abs(value - 1.0) < 10.0 * DBL_EPSILON;
    }
  }
}
