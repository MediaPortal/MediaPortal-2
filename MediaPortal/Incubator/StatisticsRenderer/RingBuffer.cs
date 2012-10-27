#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.Plugins.StatisticsRenderer
{
  /// <summary>
  /// Generic RingBuffer class.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class RingBuffer<T>
  {
    private readonly int _numberOfElements;
    private readonly T[] _items;
    private int _writeIndex;

    public RingBuffer(int numberOfElements) : this(numberOfElements, false)
    { }

    public RingBuffer(int numberOfElements, bool fillFromEnd)
    {
      _numberOfElements = numberOfElements;
      _writeIndex = fillFromEnd ? numberOfElements - 1 : 0;
      _items = new T[_numberOfElements];
    }

    public void Push(T item)
    {
      _items[_writeIndex] = item;
      _writeIndex = (_writeIndex + 1)%_numberOfElements;
    }

    public IEnumerable<T> ReadAll(int startOffset)
    {
      for (int index = startOffset; index < startOffset + _numberOfElements; index++)
        yield return _items[index % _numberOfElements];
    }
  }
}
