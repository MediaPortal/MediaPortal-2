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

using MediaPortal.Extensions.OnlineLibraries.Matches;
using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public class SimpleNameMatcher
  {
    protected MatchStorage<SimpleNameMatch, string> _storage;

    public SimpleNameMatcher(string settingsFile)
    {
      _storage = new MatchStorage<SimpleNameMatch, string>(settingsFile);
    }

    public bool GetNameMatch(string name, out string id)
    {
      id = null;

      List<SimpleNameMatch> matches = _storage.GetMatches();
      SimpleNameMatch match = matches.Find(m =>
        string.Equals(m.ItemName, name, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(m.OnlineName, name, StringComparison.OrdinalIgnoreCase));

      if (match != null)
        id = match.Id;

      return id != null;
    }

    public void StoreNameMatch(string id, string searchName, string OnlineName)
    {
      var onlineMatch = new SimpleNameMatch
      {
        Id = id,
        ItemName = searchName,
        OnlineName = OnlineName,
      };
      _storage.TryAddMatch(onlineMatch);
    }
  }
}
