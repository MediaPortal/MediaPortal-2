#region Copyright (C) 2012 MPExtended

// Copyright (C) 2012 MPExtended Developers, http://mpextended.github.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.Extensions
{
  public static class NaturalSorting
  {
    private class StringsAndIntComparer : IComparer<object>
    {
      public int Compare(object x, object y)
      {
        bool xInt = x is int;
        bool yInt = y is int;
        if (xInt && yInt) return Comparer<int>.Default.Compare((int)x, (int)y);
        if (xInt && !yInt) return -1;
        if (!xInt && yInt) return 1;

        return StringComparer.CurrentCultureIgnoreCase.Compare(x.ToString(), y.ToString());
      }
    }

    // Inspired by an algorithm from Ian Griffiths, http://www.interact-sw.co.uk/iangblog/2007/12/13/natural-sorting
    // and Jeff Atwood, http://www.codinghorror.com/blog/2007/12/sorting-for-humans-natural-sort-order.html
    //
    // IMPORTANT: If you update this code, please check if the unit tests still work. You can very easily break this code in
    // a subtle way without even noticing it. Also, please add new unit tests if you add features.

    private static Dictionary<char, int> _romanNumerals = new Dictionary<char, int>()
    {
      { 'I', 1 },
      { 'V', 5 },
      { 'X', 10 },
      { 'L', 50 },
      { 'C', 100 },
      { 'D', 500 },
      { 'M', 1000 }
    };

    private static Regex _stripPrefixRegex = new Regex(@"^\s*((a|an|the)\s+)?(.*)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static Regex _changeRoman = new Regex(@"(\s+|^)([IVXLCDM]+)(\s+|$)", RegexOptions.Compiled);
    private static Regex _splitNumerals = new Regex(@"([0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static IComparer<IEnumerable<object>> _objectComparer = new EnumerableComparer<object>(new StringsAndIntComparer());

    private static int RomanToDecimal(string roman)
    {
      int maxValue = 0, outValue = 0;
      var reversedValues = roman.ToCharArray().Select(x => _romanNumerals[x]).Reverse();
      foreach (var thisValue in reversedValues)
      {
        outValue += thisValue >= maxValue ? thisValue : -thisValue;
        maxValue = Math.Max(thisValue, maxValue);
      }

      return outValue;
    }

    private static Func<TSource, IEnumerable<object>> GetSortFunction<TSource>(Func<TSource, string> keySelector)
    {
      return inputValue =>
      {
        string strippedValue = _stripPrefixRegex.Match(keySelector(inputValue)).Groups[3].Value;
        string sortableValue = _changeRoman.Replace(strippedValue, m => String.Format("{0}{1}{2}", m.Groups[1], RomanToDecimal(m.Groups[2].Value), m.Groups[3]));
        return _splitNumerals.Split(sortableValue).Select(x =>
        {
          int val;
          return Int32.TryParse(x, out val) ? (object)val : (object)x;
        });
      };
    }

    public static IOrderedEnumerable<TSource> OrderByNatural<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector, WebSortOrder? order)
    {
      return OrderByNatural(source, keySelector, order.HasValue ? order.Value : WebSortOrder.Asc);
    }

    public static IOrderedEnumerable<TSource> OrderByNatural<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector, WebSortOrder order)
    {
      if (order == WebSortOrder.Asc)
        return Enumerable.OrderBy(source, GetSortFunction(keySelector), _objectComparer);
      return Enumerable.OrderByDescending(source, GetSortFunction(keySelector), _objectComparer);
    }

    public static IOrderedQueryable<TSource> OrderByNatural<TSource>(this IQueryable<TSource> source, Func<TSource, string> keySelector, WebSortOrder? order)
    {
      return OrderByNatural(source, keySelector, order.HasValue ? order.Value : WebSortOrder.Asc);
    }

    public static IOrderedQueryable<TSource> OrderByNatural<TSource>(this IQueryable<TSource> source, Func<TSource, string> keySelector, WebSortOrder order)
    {
      var sortFunction = GetSortFunction(keySelector);
      if (order == WebSortOrder.Asc)
        return Queryable.OrderBy(source, x => sortFunction(x), _objectComparer);
      return Queryable.OrderByDescending(source, x => sortFunction(x), _objectComparer);
    }
  }
}