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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.FanArt;

namespace Tests.Server.FanArt.MockFanArtAccess
{
  class MockFanArtCache : IFanArtCache
  {
    IDictionary<Guid, List<string>> _fanArt = new Dictionary<Guid, List<string>>();

    public IDictionary<Guid, List<string>> FanArt => _fanArt;

    public void Clear()
    {
      _fanArt.Clear();
    }

    public void DeleteFanArtFiles(Guid mediaItemId)
    {
      throw new NotImplementedException();
    }

    public ICollection<Guid> GetAllFanArtIds()
    {
      throw new NotImplementedException();
    }

    public IList<string> GetFanArtFiles(Guid mediaItemId, string fanArtType)
    {
      throw new NotImplementedException();
    }

    public Task<bool> TrySaveFanArt(Guid mediaItemId, string title, string fanArtType, TrySaveFanArtAsyncDelegate saveDlgt)
    {
      throw new NotImplementedException();
    }

    public Task<int> TrySaveFanArt<T>(Guid mediaItemId, string title, string fanArtType, ICollection<T> files, TrySaveMultipleFanArtAsyncDelegate<T> saveDlgt)
    {
      if (files != null && files.Count > 0)
        GetOrAddFanArtPathList(mediaItemId).AddRange(files.Select(f => f.ToString()));
      return Task.FromResult(files != null ? files.Count : 0);
    }

    protected List<string> GetOrAddFanArtPathList(Guid mediaItemId)
    {
      List<string> list;
      if (!_fanArt.TryGetValue(mediaItemId, out list))
        _fanArt[mediaItemId] = list = new List<string>();
      return list;
    }
  }
}
