#region Copyright (C) 2007 Ian Griffiths, 2013 MPExtended (MIT-licensed)

// Copyright (C) 2013 MPExtended Developers, http://mpextended.github.com/
// Copyright (C) 2007 Ian Griffiths, http://www.interact-sw.co.uk/iangblog/2007/12/13/natural-sorting
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

#endregion

using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
  public class EnumerableComparer<T> : IComparer<IEnumerable<T>>
  {
    private IComparer<T> itemComparer;

    public EnumerableComparer(IComparer<T> comparer)
    {
      itemComparer = comparer;
    }

    public EnumerableComparer()
      : this(Comparer<T>.Default)
    {
    }

    /// <summary>
    /// Compare two sequences of T, using the itemComparer function to compare each item.
    /// 
    /// It works like this:
    /// - Try to take an item from both lists
    /// - If both lists don't have an item left, the lists are equal
    /// - If x doesn't have any items left, but y does, treat x as less than y
    /// - If y doesn't have any items left, but x does, treat x as greater than y
    /// - If the items taken from both lists aren't equal, compare them
    ///   (i.e. if the item from x is less than the one from y, x is treated as less than y)
    /// - Repeat
    /// </summary>
    public int Compare(IEnumerable<T> x, IEnumerable<T> y)
    {
      using (IEnumerator<T> xIterator = x.GetEnumerator())
      {
        using (IEnumerator<T> yIterator = y.GetEnumerator())
        {
          while (true)
          {
            bool xNext = xIterator.MoveNext();
            bool yNext = yIterator.MoveNext();

            if (!xNext && !yNext) return 0;
            if (!xNext) return -1;
            if (!yNext) return 1;

            int itemResult = itemComparer.Compare(xIterator.Current, yIterator.Current);
            if (itemResult != 0) return itemResult;
          }
        }
      }
    }
  }
}