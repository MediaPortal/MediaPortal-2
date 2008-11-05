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

namespace MediaPortal.Utilities
{
  public class CollectionUtils
  {
    /// <summary>
    /// Adds all elements in the <paramref name="source"/> enumeration to the <paramref name="target"/> collection.
    /// </summary>
    /// <typeparam name="S">Type of the source elements. Needs to be equal to <see cref="T"/> or to be a
    /// sub type.</typeparam>
    /// <typeparam name="T">Target type.</typeparam>
    /// <param name="target">Target collection where all elements from <paramref name="source"/> will be added.</param>
    /// <param name="source">Source enumeration whose elements will be added to <paramref name="target"/>.</param>
    public static void AddAll<S, T>(ICollection<T> target, IEnumerable<S> source) where S: T
    {
      foreach (S s in source)
        target.Add(s);
    }

    /// <summary>
    /// Calculates the intersection of <paramref name="c1"/> and <paramref name="c2"/> and returns it.
    /// If the type parameters of the collections differ, the collection with the more general element type
    /// must be used at the second position.
    /// </summary>
    /// <typeparam name="S">Element type of the first source collection. May be more specific than
    /// the type parameter of the second collection.</typeparam>
    /// <typeparam name="T">Element type of the second source collection and the result collection.
    /// May be more general than the type parameter of the first collection <see cref="S"/>.</typeparam>
    /// <param name="c1">First source collection.</param>
    /// <param name="c2">Second source collection</param>
    /// <returns>Intersection of <paramref name="c1"/> and <paramref name="c2"/>.</returns>
    [Obsolete("Can be replaced by Intersect extension method since .net 3.5")] 
    public static ICollection<T> Intersection<S, T>(ICollection<S> c1, ICollection<T> c2) where S: T
    {
      ICollection<T> result = new List<T>();
      foreach (S s in c1)
        if (c2.Contains(s))
          result.Add(s);
      return result;
    }
  }
}