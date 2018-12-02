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

using System.Collections.Generic;

namespace MediaPortal.Utilities.Collections
{
  /// <summary>
  /// Helper class that acts like a regular <see cref="List{T}"/>, with exception of constructor which doesn't throw on <c>null</c> collection.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class SafeList<T> : List<T>
  {
    public SafeList()
    {
    }

    /// <summary>
    /// Constructs a new list based on the <paramref name="otherList"/>. If <paramref name="otherList"/> is <c>null</c>, no Exception is thrown.
    /// </summary>
    /// <param name="otherList">Other items or <c>null</c></param>
    public SafeList(IEnumerable<T> otherList)
    {
      if (otherList != null)
        AddRange(otherList);
    }
  }
}
