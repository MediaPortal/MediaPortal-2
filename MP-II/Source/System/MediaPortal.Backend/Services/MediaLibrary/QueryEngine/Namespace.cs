#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Represents a mapping of object identities to unique names within this namespace.
  /// </summary>
  public class Namespace
  {
    protected readonly IDictionary<object, string> _objects2names = new Dictionary<object, string>();
    protected readonly ICollection<string> namesIndex = new HashSet<string>();

    public string Get(object obj)
    {
      string result;
      if (_objects2names.TryGetValue(obj, out result))
        return result;
      return null;
    }

    public string GetOrCreate(object obj, string prefix)
    {
      string result = Get(obj);
      if (!string.IsNullOrEmpty(result))
        return result;
      int ct = 0;
      while (namesIndex.Contains(result = string.Format("{0}{1}", prefix, ct)))
        ct++;
      namesIndex.Add(result);
      return _objects2names[obj] = result;
    }
  }
}
