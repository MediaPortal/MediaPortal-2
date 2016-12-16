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

using System;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="BaseInfo"/> contains metadata information about a thumbnail item.
  /// </summary>
  public class GenreInfo : IComparable<GenreInfo>
  {
    public int? Id;
    public string Name;

    public override string ToString()
    {
      return string.IsNullOrEmpty(Name) ? "[Unnamed Genre]" : Name;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      return ToString().GetHashCode();
    }

    public override bool Equals(object obj)
    {
      GenreInfo other = obj as GenreInfo;
      if (other == null) return false;

      if(!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name))
        return string.Compare(Name, other.Name, true) == 0;

      return false;
    }

    public int CompareTo(GenreInfo other)
    {
      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name))
        return string.Compare(Name, other.Name, true);

      return -1;
    }
  }
}
