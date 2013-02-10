#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;

namespace MediaPortal.Extensions.OnlineLibraries.Matches
{
  // TODO: implement lookup table and download stats in database
  /// <summary>
  /// Storage class for loading and saving <see cref="BaseMatch{T}"/> into storage, which is currently 
  /// a XML serialized file in the data folder of the application.
  /// </summary>
  /// <typeparam name="TMatch">Type of match</typeparam>
  /// <typeparam name="TId">Type of match's ID</typeparam>
  public class MatchStorage<TMatch, TId> where TMatch : BaseMatch<TId>
  {
    protected Predicate<TMatch> _matchPredicate;
    protected readonly string _matchesSettingsFile;
    protected readonly object _syncObj = new object();

    public MatchStorage(string matchesSettingsFile)
    {
      _matchesSettingsFile = matchesSettingsFile;
    }

    public List<TMatch> LoadMatches()
    {
      lock (_syncObj)
        return Settings.Load<List<TMatch>>(_matchesSettingsFile) ?? new List<TMatch>();
    }

    public void SaveMatches(List<TMatch> matches)
    {
      Settings.Save(_matchesSettingsFile, matches);
    }

    public void SaveNewMatch(string itemName, TMatch onlineMatch)
    {
      lock (_syncObj)
      {
        List<TMatch> matches = LoadMatches();
        if (matches.Any(m => m.ItemName == itemName))
          return;
        matches.Add(onlineMatch);
        SaveMatches(matches);
      }
    }
  }
}
