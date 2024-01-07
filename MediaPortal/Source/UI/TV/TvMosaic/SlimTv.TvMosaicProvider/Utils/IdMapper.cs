#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace SlimTv.TvMosaicProvider.Utils
{
  /// <summary>
  /// Maps a key to a unique integer id.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  internal class IdMapper<T>
  {
    private readonly ConcurrentDictionary<T, int> _idMapping = new ConcurrentDictionary<T, int>();
    private int _lastId = 0;

    public int GetOrCreateId(T key)
    {
      if(_idMapping.TryGetValue(key, out int id))
        return id;

      return _idMapping.GetOrAdd(key, k => Interlocked.Increment(ref _lastId));
    }

    public T GetKey(int id)
    {
      return _idMapping.FirstOrDefault(kvp => kvp.Value == id).Key;
    }
  }
}
