#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
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
    protected readonly string _matchesSettingsFile;
    protected readonly ConcurrentDictionary<String, TMatch> _storage;

    protected readonly object _syncObj = new object();

    public MatchStorage(string matchesSettingsFile)
    {
      _matchesSettingsFile = matchesSettingsFile;
      _storage = new ConcurrentDictionary<string, TMatch>(StringComparer.OrdinalIgnoreCase);

      var matches = Settings.Load<List<TMatch>>(_matchesSettingsFile) ?? new List<TMatch>();
      foreach (var match in matches)
        _storage[match.ItemName] = match;
    }

    public bool TryAddMatch(TMatch match)
    {
      if (_storage.TryAdd(match.ItemName, match))
      {
        SaveMatchesAsync();
        return true;
      }
      return false;
    }

    public List<TMatch> GetMatches()
    {
      return _storage.Values.ToList();
    }

    public Task SaveMatchesAsync()
    {
      var saveTask = Task.Run(() =>
      {
        var matchList = _storage.Values.ToList();
        lock (_syncObj)
          Settings.Save(_matchesSettingsFile, matchList);
      });
      saveTask.ContinueWith(previousTask => ServiceRegistration.Get<ILogger>().Error("MatchStorage: Error writing storage file {0}", _matchesSettingsFile), TaskContinuationOptions.OnlyOnFaulted);
      return saveTask;
    }

  }
}
