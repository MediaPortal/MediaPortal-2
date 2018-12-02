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
using System.Collections.Generic;

namespace MediaPortal.Utilities
{
  public static class ObjectUtils
  {
    /// <summary>
    /// Checks if the two objects <paramref name="o1"/> and <paramref name="o2"/> are equal based on
    /// a comparer.
    /// This method also copes with <c>null</c> values, i.e. if both objects are <c>null</c>, the
    /// method will return <c>true</c>.
    /// </summary>
    /// <typeparam name="T">Type of the objects.</typeparam>
    /// <param name="o1">First object to compare.</param>
    /// <param name="o2">Second object to compare.</param>
    /// <param name="comparer">Equality comparer method to compute object equality.</param>
    /// <returns><c>true</c>, if both objects are <c>null</c> or if <paramref name="o1"/> is equal
    /// to <paramref name="o2"/>, based on the result of the <see cref="IEqualityComparer{T}.Equals(T,T)"/>
    /// method.</returns>
    public static bool ObjectsAreEqual<T>(T o1, T o2, IEqualityComparer<T> comparer)
        where T : class
    {
      if (o1 == null && o2 == null)
        return true;
      if (o1 == null || o2 == null)
        return false;
      return comparer.Equals(o1, o2);
    }

    public static int Compare<T>(T x, T y) where T : class, IComparable<T>
    {
      if (x == null && y == null)
        return 0;
      if (x == null)
        return -1;
      if (y == null)
        return 1;
      return x.CompareTo(y);
    }

    public static int Compare<T>(T? x, T? y) where T : struct, IComparable<T>
    {
      if (!x.HasValue && !y.HasValue)
        return 0;
      if (!x.HasValue)
        return -1;
      if (!y.HasValue)
        return 1;
      return x.Value.CompareTo(y.Value);
    }
  }
}