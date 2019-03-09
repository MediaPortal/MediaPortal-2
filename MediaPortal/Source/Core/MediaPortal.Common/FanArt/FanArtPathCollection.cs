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

using MediaPortal.Common.ResourceAccess;
using System.Collections;
using System.Collections.Generic;

namespace MediaPortal.Common.FanArt
{
  public class FanArtPathCollection : IEnumerable<KeyValuePair<string, List<ResourcePath>>>
  {
    protected IDictionary<string, List<ResourcePath>> _pathsMap = new Dictionary<string, List<ResourcePath>>();

    public void Add(string fanArtType, ResourcePath path)
    {
      if (path == null)
        return;
      List<ResourcePath> typePaths = GetOrAddPathList(fanArtType);
      typePaths.Add(path);
    }

    public void AddRange(string fanArtType, ICollection<ResourcePath> paths)
    {
      if (paths == null || paths.Count == 0)
        return;
      List<ResourcePath> typePaths = GetOrAddPathList(fanArtType);
      typePaths.AddRange(paths);
    }

    public void AddRange(FanArtPathCollection collection)
    {
      if (collection == null)
        return;
      foreach (var fanArtPath in collection.Paths)
      {
        string fanArtType = fanArtPath.Key;
        List<ResourcePath> typePaths = GetOrAddPathList(fanArtType);
        typePaths.AddRange(fanArtPath.Value);
      }
    }

    public int Count(string fanArtType)
    {
      List<ResourcePath> paths;
      if (_pathsMap.TryGetValue(fanArtType, out paths))
        return paths.Count;
      return 0;
    }

    public IDictionary<string, List<ResourcePath>> Paths
    {
      get { return _pathsMap; }
    }

    protected List<ResourcePath> GetOrAddPathList(string fanArtType)
    {
      List<ResourcePath> paths;
      if (!_pathsMap.TryGetValue(fanArtType, out paths))
        _pathsMap[fanArtType] = paths = new List<ResourcePath>();
      return paths;
    }

    public IEnumerator<KeyValuePair<string, List<ResourcePath>>> GetEnumerator()
    {
      return _pathsMap.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return ((IEnumerable)_pathsMap).GetEnumerator();
    }
  }
}
