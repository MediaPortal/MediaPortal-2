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
using System.Collections;
using System.Collections.Generic;

namespace MediaPortal.Utilities
{
  public static class CollectionUtils
  {
    /// <summary>
    /// Transformer which takes an object of type <typeparamref name="S"/> and transforms it to type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="S">Source object to transform.</typeparam>
    /// <typeparam name="T">Target object of the transformation.</typeparam>
    public interface ITransformer<S, T>
    {
      /// <summary>
      /// Transforms <paramref name="source"/> into a result object.
      /// </summary>
      /// <param name="source">Object to transform.</param>
      /// <returns>Target object of the transformation.</returns>
      T Transform(S source);
    }

    /// <summary>
    /// Removes all elements in the <paramref name="source"/> enumeration from the <paramref name="target"/> collection.
    /// </summary>
    /// <typeparam name="S">Type of the source elements. Needs to be equal to <see cref="T"/> or to be a
    /// sub type.</typeparam>
    /// <typeparam name="T">Target type.</typeparam>
    /// <param name="target">Target collection where all elements from <paramref name="source"/> will be removed.</param>
    /// <param name="source">Source enumeration whose elements will be removed from <paramref name="target"/>.</param>
    public static void RemoveAll<S, T>(ICollection<T> target, IEnumerable<S> source) where S: T
    {
      foreach (S s in source)
        target.Remove(s);
    }

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
    /// Adds all objects in the <paramref name="source"/> enumeration to the <paramref name="target"/> collection.
    /// </summary>
    /// <param name="target">Target collection where all elements from <paramref name="source"/> will be added.</param>
    /// <param name="source">Source enumeration whose elements will be added to <paramref name="target"/>.</param>
    public static void AddAll(ICollection<object> target, IEnumerable source)
    {
      foreach (object o in source)
        target.Add(o);
    }

    /// <summary>
    /// Calculates the union list of <paramref name="c1"/> and <paramref name="c2"/> and returns it. The elements
    /// of the two given enumerations of elements will all be added, first the elements of <paramref name="c1"/>,
    /// then the elements of <paramref name="c2"/>.
    /// If the type parameters of the collections differ, the collection with the more general element type
    /// must be used at the second position.
    /// </summary>
    /// <remarks>
    /// This method executes in O(sizeof(c1) + sizeof(c2)).
    /// </remarks>
    /// <typeparam name="S">Element type of the first source collection. May be more specific than
    /// the type parameter of the second collection.</typeparam>
    /// <typeparam name="T">Element type of the second source collection and the result collection.
    /// May be more general than the type parameter of the first collection <see cref="S"/>.</typeparam>
    /// <param name="c1">First source collection.</param>
    /// <param name="c2">Second source collection</param>
    /// <returns>Union set of <paramref name="c1"/> and <paramref name="c2"/>.</returns>
    public static IList<T> UnionList<S, T>(IEnumerable<S> c1, IEnumerable<T> c2) where S: T
    {
      IList<T> result = new List<T>();
      AddAll(result, c1);
      AddAll(result, c2);
      return result;
    }

    /// <summary>
    /// Calculates the union set of <paramref name="c1"/> and <paramref name="c2"/> and returns it. The result set
    /// will contain all elements of <paramref name="c1"/> and those arguements of <paramref name="c2"/> which aren't
    /// present in <paramref name="c1"/>.
    /// If the type parameters of the collections differ, the collection with the more general element type
    /// must be used at the second position.
    /// </summary>
    /// <remarks>
    /// This method executes in O(sizeof(c1) * sizeof(c2)).
    /// </remarks>
    /// <typeparam name="S">Element type of the first source collection. May be more specific than
    /// the type parameter of the second collection.</typeparam>
    /// <typeparam name="T">Element type of the second source collection and the result collection.
    /// May be more general than the type parameter of the first collection <see cref="S"/>.</typeparam>
    /// <param name="c1">First source collection.</param>
    /// <param name="c2">Second source collection</param>
    /// <returns>Union set of <paramref name="c1"/> and <paramref name="c2"/>.</returns>
    public static ICollection<T> UnionSet<S, T>(IEnumerable<S> c1, IEnumerable<T> c2) where S: T
    {
      ICollection<T> result = new HashSet<T>();
      AddAll(result, c1);
      AddAll(result, c2);
      return result;
    }

    /// <summary>
    /// Calculates the intersection set of <paramref name="c1"/> and <paramref name="c2"/> and returns it.
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
    public static ICollection<T> Intersection<S, T>(IEnumerable<S> c1, IEnumerable<T> c2) where S: T
    {
      ICollection<T> result = new List<T>();
      ICollection<S> x1 = c1 as ICollection<S>;
      if (x1 != null)
      { // First argument implements ICollection<S>
        foreach (S s in c2)
          if (x1.Contains(s))
            result.Add(s);
      }
      else
      {
        ICollection<T> x2 = c2 as ICollection<T> ?? new List<T>(c2); // If second argument also doesn't implement ICollection<T>, create a new list
        foreach (S s in c1)
          if (x2.Contains(s))
            result.Add(s);
      }
      return result;
    }

    /// <summary>
    /// Returns the information whether the intersection of <paramref name="c1"/> and <paramref name="c2"/> is not empty.
    /// If the type parameters of the collections differ, the collection with the more general element type
    /// must be used at the second position.
    /// </summary>
    /// <typeparam name="S">Element type of the first source collection. May be more specific than
    /// the type parameter of the second collection.</typeparam>
    /// <typeparam name="T">Element type of the second source collection and the result collection.
    /// May be more general than the type parameter of the first collection <see cref="S"/>.</typeparam>
    /// <param name="c1">First source collection.</param>
    /// <param name="c2">Second source collection</param>
    /// <returns><c>true</c>, if the intersection of <paramref name="c1"/> and <paramref name="c2"/> is not empty.
    /// Else <c>false</c>.</returns>
    public static bool HasIntersection<S, T>(ICollection<S> c1, ICollection<T> c2) where S: T
    {
      foreach (S s in c1)
        if (c2.Contains(s))
          return true;
      return false;
    }

    /// <summary>
    /// Compares two sets of elements with a reference type given by the two enumerations
    /// <paramref name="e1"/> and <paramref name="e2"/>. If the sets differ in size or in at least one
    /// element, the return value will be <c>false</c>, else it will be <c>true</c>.
    /// The comparison is based on the <see cref="object.Equals(object)"/> method of the objects in
    /// <paramref name="e1"/>. The comparison does NOT compare the ORDER of elements, so for two enumerations
    /// with the same items but different order, this method will return <c>true</c>.
    /// </summary>
    /// <typeparam name="S">Element type of the first enumeration.</typeparam>
    /// <typeparam name="T">Element type of the second enumeration.</typeparam>
    /// <param name="e1">First element enumeration to compare.</param>
    /// <param name="e2">Second element enumeration to compare.</param>
    /// <returns><c>true</c>, if the size and all elements of the enumerations are the same,
    /// else <c>false</c>. The order of elements doesn't matter in the enumerations.</returns>
    public static bool CompareObjectCollections<S, T>(IEnumerable<S> e1, IEnumerable<T> e2)
        where S : class
        where T : class, S
    {
      List<S> l1 = new List<S>(e1);
      List<T> l2 = new List<T>(e2);
      if (l1.Count != l2.Count)
        return false;
      l1.Sort();
      l2.Sort();
      for (int i=0; i<l1.Count; i++)
        if (!Equals(l1[i], l2[i]))
          return false;
      return true;
    }

    /// <summary>
    /// Returns the indexth element of the specified <paramref name="list"/>. If the list is null or empty or if the index
    /// is outside the list's bounds, <c>null</c> will be returned.
    /// </summary>
    /// <typeparam name="T">Type of the list's elements.</typeparam>
    /// <param name="list">The list to access.</param>
    /// <param name="index">The index of the list to access.</param>
    /// <returns><c>list[index]</c> or <c>null</c>.</returns>
    public static T SafeGet<T>(IList<T> list, int index) where T : class
    {
      if (list == null || list.Count == 0 || index < 0 || index >= list.Count)
        return null;
      return list[index];
    }

    private class ComparisonEqualityComparer<T> : IEqualityComparer<T>
    {
      private readonly Comparison<T> _comparison;

      public ComparisonEqualityComparer(Comparison<T> comparison)
      {
        _comparison = comparison;
      }

      #region Implementation of IEqualityComparer<T>

      public bool Equals(T x, T y)
      {
        return _comparison(x, y) == 0;
      }

      public int GetHashCode(T obj)
      {
        return 0;
      }

      #endregion
    }

    /// <summary>
    /// Compares two sets of elements with a reference type given by the two enumerations
    /// <paramref name="e1"/> and <paramref name="e2"/>. If the sets differ in size or in at least one
    /// element, the return value will be <c>false</c>, else it will be <c>true</c>.
    /// The comparison is based on the return value of the <paramref name="comparison"/> delegate.
    /// The comparison does NOT compare the ORDER of elements, so for two enumerations with the same items but
    /// different order, this method will return <c>true</c>.
    /// </summary>
    /// <typeparam name="S">Element type of the first enumeration.</typeparam>
    /// <typeparam name="T">Element type of the second enumeration.</typeparam>
    /// <param name="e1">First element enumeration to compare.</param>
    /// <param name="e2">Second element enumeration to compare.</param>
    /// <param name="comparison">Comparison method used to compare the objects.</param>
    /// <returns><c>true</c>, if the size and all elements of the enumerations are the same,
    /// else <c>false</c>. The order of elements doesn't matter in the enumerations.</returns>
    public static bool CompareObjectCollections<S, T>(IEnumerable<S> e1, IEnumerable<T> e2,
        Comparison<S> comparison)
      where S : class
      where T : class, S
    {
      List<S> l1 = new List<S>(e1);
      List<S> l2 = new List<S>(l1.Count);
      AddAll(l2, e2);
      if (l1.Count != l2.Count)
        return false;
      l1.Sort(comparison);
      l2.Sort(comparison);
      ComparisonEqualityComparer<S> cec = new ComparisonEqualityComparer<S>(comparison);
      for (int i = 0; i < l1.Count; i++)
        if (!ObjectUtils.ObjectsAreEqual(l1[i], l2[i], cec))
          return false;
      return true;
    }

    /// <summary>
    /// Compares two sets of elements with a non-reference type (i.e. struct or enum) given by the two
    /// enumerations <paramref name="e1"/> and <paramref name="e2"/>. If the sets differ in size or in at
    /// least one element, the return value will be <c>false</c>, else it will be <c>true</c>.
    /// The comparison is based on the return value of the <paramref name="comparison"/> delegate.
    /// The comparison does NOT compare the ORDER of elements, so for two enumerations with the same items but
    /// different order, this method will return <c>true</c>.
    /// </summary>
    /// <typeparam name="T">Element type of the enumeration.</typeparam>
    /// <param name="e1">First element enumeration to compare.</param>
    /// <param name="e2">Second element enumeration to compare.</param>
    /// <param name="comparison">Comparison method used to compare the objects.</param>
    /// <returns><c>true</c>, if the size and all elements of the enumerations are the same,
    /// else <c>false</c>. The order of elements doesn't matter in the enumerations.</returns>
    public static bool CompareCollections<T>(IEnumerable<T> e1, IEnumerable<T> e2,
        Comparison<T> comparison) where T : struct
    {
      List<T> l1 = new List<T>(e1);
      List<T> l2 = new List<T>(e2);
      if (l1.Count != l2.Count)
        return false;
      l1.Sort(comparison);
      l2.Sort(comparison);
      for (int i = 0; i < l1.Count; i++)
        if (comparison(l1[i], l2[i]) != 0)
          return false;
      return true;
    }

    /// <summary>
    /// Exchanges the items at index <paramref name="index1"/> and <paramref name="index2"/> in the specified
    /// <paramref name="list"/>.
    /// </summary>
    /// <typeparam name="T">Type of items in the list.</typeparam>
    /// <param name="list">List whose items should be swapped.</param>
    /// <param name="index1">First index to exchange.</param>
    /// <param name="index2">Second index to exchange.</param>
    public static void Swap<T>(IList<T> list, int index1, int index2)
    {
      T tmp = list[index1];
      list[index1] = list[index2];
      list[index2] = tmp;
    }

    /// <summary>
    /// Clusters the given enumeration into clusters of the given <paramref name="clusterSize"/>.
    /// </summary>
    /// <typeparam name="T">Type of the elements in the enumeration to cluster.</typeparam>
    /// <param name="enumeration">Elements to cluster.</param>
    /// <param name="clusterSize">Size of the clusters which should be created.</param>
    /// <returns>Collection of lists, each of size <paramref name="clusterSize"/> except the last one, which contains the
    /// rest of the elements.</returns>
    public static ICollection<IList<T>> Cluster<T>(IEnumerable<T> enumeration, int clusterSize)
    {
      List<T> elements = new List<T>(enumeration);
      int clusterCount = (elements.Count - 1) / clusterSize + 1;
      ICollection<IList<T>> result = new List<IList<T>>(clusterCount);
      for (int i = 0; i < clusterCount; i++)
        result.Add(elements.GetRange(i * clusterSize, clusterSize));
      return result;
    }
  }
}